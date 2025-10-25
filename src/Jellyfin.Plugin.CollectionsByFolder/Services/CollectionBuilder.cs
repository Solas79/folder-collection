using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionsByFolder.Services
{
    /// <summary>
    /// Baut/aktualisiert Collections anhand der gespeicherten Plugin-Konfiguration.
    /// Diese Version versucht KEINEN direkten Zugriff auf interne Jellyfin-Implementierungen
    /// (z.B. BaseItemRepository), damit das Plugin ohne Server-internes DLL-Referenzieren
    /// gebaut und verteilt werden kann.
    /// </summary>
    public sealed class CollectionBuilder
    {
        private readonly ILibraryManager _library;
        private readonly ICollectionManager _collections;
        private readonly ILogger<CollectionBuilder> _log;

        public CollectionBuilder(
            ILibraryManager library,
            ICollectionManager collections,
            ILogger<CollectionBuilder> log)
        {
            _library = library;
            _collections = collections;
            _log = log;
        }

        public async Task<(int created, int updated)> RunOnceAsync(
            PluginConfiguration cfg,
            CancellationToken ct = default)
        {
            var wl = (cfg.Whitelist ?? new List<string>())
                .Select(NormEndSlash)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var bl = (cfg.Blacklist ?? new List<string>())
                .Select(NormEndSlash)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var min    = Math.Max(1, cfg.MinFiles);
            var prefix = cfg.Prefix ?? string.Empty;
            var suffix = cfg.Suffix ?? string.Empty;

            _log.LogInformation("[CBF] Scan start: WL={W} BL={B} Min={Min} Prefix='{P}' Suffix='{S}'",
                wl.Count, bl.Count, min, prefix, suffix);

            // 1. Alle Movies im System sammeln, ohne GetItemList().
            //    Strategie:
            //    - Wir holen alle Root-Library-Items.
            //    - Wir laufen rekursiv runter und sammeln Movie-Instanzen.
            var movies = GetAllMoviesFallback(ct).ToList();

            _log.LogInformation("[CBF] Movies gesamt (fallback traversal): {N}", movies.Count);

            bool WL(string p) =>
                wl.Count == 0 ||
                wl.Any(w => p.StartsWith(w, StringComparison.OrdinalIgnoreCase));

            bool BL(string p) =>
                bl.Any(b => p.StartsWith(b, StringComparison.OrdinalIgnoreCase));

            var filtered = movies
                .Where(m =>
                {
                    var p = m.Path ?? "";
                    return WL(p) && !BL(p);
                })
                .ToList();

            _log.LogInformation("[CBF] Movies nach WL/BL: {N}", filtered.Count);

            // WL-Roots ohne Slash-Ende zum direkten Ausschluss
            var wlRootsNoSlash = new HashSet<string>(
                wl.Select(s => s.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                StringComparer.OrdinalIgnoreCase);

            // Gruppieren nach Parent-Ordner; WL-Roots selbst NICHT zu Collections machen
            var groups = filtered
                .GroupBy(m => (Path.GetDirectoryName(m.Path ?? "") ?? "")
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                .Where(g => g.Key.Length > 0 && !wlRootsNoSlash.Contains(g.Key))
                .ToList();

            _log.LogInformation("[CBF] Ordner-Gruppen gesamt (ohne WL-Root): {N}", groups.Count);

            int created = 0, updated = 0;

            foreach (var g in groups)
            {
                ct.ThrowIfCancellationRequested();

                if (g.Count() < min)
                    continue;

                var folderName = Path.GetFileName(g.Key);
                if (string.IsNullOrWhiteSpace(folderName))
                    continue;

                var name = $"{prefix}{folderName}{suffix}";
                var items = g.Cast<BaseItem>().ToList();
                var itemIds = items.Select(i => i.Id).ToList();

                // 2. Gibt es schon eine Collection mit genau diesem Namen?
                //    Wir können nicht mehr via _library.GetItemList(query) suchen.
                //    Workaround:
                //    - Durchsuche alle vorhandenen BoxSets, die wir über die Wurzelknoten finden.
                //      (BoxSet = Sammlungen / Collections / Sets)
                //    - Oder notfalls: Finde eine existierende Collection durch _collections,
                //      indem wir alle Collections holen können, dann Name vergleichen.

                var existing = FindExistingBoxSetByName(name);

                if (existing == null)
                {
                    _log.LogInformation("[CBF] Create Collection '{Name}' (Items={C})", name, items.Count);

                    var createdOk =
                           await TryCreateCollection_OptionsAsync(_collections, name, itemIds, ct).ConfigureAwait(false)
                        || await TryCreateCollection_OptionsAsync(_library,     name, itemIds, ct).ConfigureAwait(false);

                    if (!createdOk)
                    {
                        _log.LogWarning("[CBF] CreateCollection API nicht gefunden/fehlgeschlagen für '{Name}'", name);
                        DumpAvailableApis();
                        continue;
                    }

                    created++;

                    // Sammlung nach Create nochmal suchen:
                    var just = FindExistingBoxSetByName(name);

                    if (just != null && itemIds.Count > 0)
                    {
                        var addOk =
                               await TryAddToCollection_AddAsync(_collections, just.Id, itemIds, ct).ConfigureAwait(false)
                            || await TryAddToCollection_AddAsync(_library,     just.Id, itemIds, ct).ConfigureAwait(false);

                        if (!addOk)
                        {
                            _log.LogWarning("[CBF] AddToCollection nach Create fehlgeschlagen für '{Name}'", name);
                        }
                    }
                    else
                    {
                        _log.LogDebug("[CBF] Newly created collection nicht gefunden oder keine Items – skip add.");
                    }
                }
                else
                {
                    var ex = new HashSet<Guid>(existing.GetLinkedChildren().Select(x => x.Id));
                    var toAdd = items.Where(x => !ex.Contains(x.Id)).ToList();
                    var toAddId = toAdd.Select(x => x.Id).ToList();

                    if (toAddId.Count > 0)
                    {
                        var ok =
                               await TryAddToCollection_AddAsync(_collections, existing.Id, toAddId, ct).ConfigureAwait(false)
                            || await TryAddToCollection_AddAsync(_library,     existing.Id, toAddId, ct).ConfigureAwait(false);

                        if (!ok)
                        {
                            _log.LogWarning("[CBF] AddToCollection API nicht gefunden/fehlgeschlagen für '{Name}'", name);
                            DumpAvailableApis();
                            continue;
                        }

                        updated++;
                    }
                }
            }

            _log.LogInformation("[CBF] Scan fertig: created={Cr} updated={Up}", created, updated);
            return (created, updated);
        }

        /// <summary>
        /// Holt alle Movies über Traversal (Fallback ohne ILibraryManager.GetItemList).
        /// Idee: Wir nehmen alle Root-Items und laufen rekursiv runter.
        /// </summary>
        private IEnumerable<Movie> GetAllMoviesFallback(CancellationToken ct)
        {
            // ILibraryManager hat normalerweise Zugriff auf Wurzel-Items (Bibliotheken).
            // Wir gehen defensive: wir holen ALLE items, die der Manager kennt,
            // filtern auf Movie, und machen es robust gegen Nulls.
            //
            // Hinweis: Je nach Jellyfin-Version gibt es:
            //   - GetItemById / RootFolder / GetChildren()
            //   - oder LibraryManager.RootFolder
            // Wir versuchen den Weg über RootFolder, weil der seit Jahren existiert.

            var root = _library.RootFolder; // RootFolder ist in MediaBrowser.Controller.Library. 
            if (root == null)
            {
                _log.LogWarning("[CBF] RootFolder ist null – kein Traversal möglich.");
                return Enumerable.Empty<Movie>();
            }

            var list = new List<Movie>();
            Traverse(root, list, ct);
            return list;

            void Traverse(BaseItem node, List<Movie> acc, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                if (node is Movie mov)
                {
                    acc.Add(mov);
                }

                // Hole Kinder des Knotens
                IEnumerable<BaseItem> children;
                try
                {
                    children = node.GetChildren() ?? Array.Empty<BaseItem>();
                }
                catch (Exception ex)
                {
                    _log.LogDebug(ex, "[CBF] GetChildren() Exception bei {Node}", node.Name);
                    children = Array.Empty<BaseItem>();
                }

                foreach (var child in children)
                {
                    Traverse(child, acc, token);
                }
            }
        }

        /// <summary>
        /// Versucht, ein vorhandenes BoxSet (Sammlung) mit genau diesem Namen zu finden.
        /// Fallback-Strategie ohne _library.GetItemList(query).
        /// </summary>
        private BoxSet? FindExistingBoxSetByName(string name)
        {
            // Idee:
            // - Wir durchsuchen ab RootFolder rekursiv nach BoxSet.
            // - Dann Name vergleichen (case-insensitive).
            //
            // Das ist nicht die allerschnellste Methode in riesigen Bibliotheken,
            // aber sie vermeidet private Jellyfin-APIs.

            var root = _library.RootFolder;
            if (root == null)
                return null;

            BoxSet? found = null;

            void Walk(BaseItem node)
            {
                if (found != null) return;

                if (node is BoxSet bs)
                {
                    if (string.Equals(bs.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        found = bs;
                        return;
                    }
                }

                IEnumerable<BaseItem> children;
                try
                {
                    children = node.GetChildren() ?? Array.Empty<BaseItem>();
                }
                catch
                {
                    return;
                }

                foreach (var c in children)
                {
                    if (found != null) break;
                    Walk(c);
                }
            }

            Walk(root);
            return found;
        }

        // ---------- Create / Add bleiben wie gehabt (Reflection, tolerant gegenüber Signatur-Änderungen) ----------

        private async Task<bool> TryCreateCollection_OptionsAsync(
            object targetManager, string name, IReadOnlyCollection<Guid> itemIds, CancellationToken ct)
        {
            var mi = targetManager.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m =>
                    m.Name.IndexOf("CreateCollectionAsync", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    m.GetParameters().Length >= 1 &&
                    m.GetParameters()[0].ParameterType.Name.IndexOf("CollectionCreationOptions",
                        StringComparison.OrdinalIgnoreCase) >= 0);

            if (mi == null) return false;

            var pars = mi.GetParameters();
            var optionsType = pars[0].ParameterType;

            var options = Activator.CreateInstance(optionsType);
            if (options == null) return false;

            if (!SetStringProperty(options, "Name", name))
            {
                SetStringProperty(options, "CollectionName", name);
            }

            if (!TrySetGuidEnumerable(options, new[] { "ItemIds", "Ids", "Items", "MediaIds" }, itemIds))
            {
                _log.LogDebug("[CBF] Options: keine Guid-Enumerable-Property gefunden – erzeuge leere Collection, füge später Items hinzu.");
            }

            var args = new List<object?> { options };
            for (int i = 1; i < pars.Length; i++)
            {
                var p = pars[i];
                if (p.ParameterType == typeof(CancellationToken)) args.Add(ct);
                else args.Add(GetDefault(p.ParameterType));
            }

            _log.LogInformation("[CBF] Create via {Sig}", FormatSignature(mi));

            try
            {
                await InvokeAsync(targetManager, mi, args.ToArray()).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "[CBF] CreateCollectionAsync fehlgeschlagen ({Sig})", FormatSignature(mi));
                return false;
            }
        }

        private async Task<bool> TryAddToCollection_AddAsync(
            object targetManager, Guid collectionId, IReadOnlyCollection<Guid> itemIds, CancellationToken ct)
        {
            var mi = targetManager.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m =>
                    m.Name.IndexOf("AddToCollectionAsync", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    m.GetParameters().Length >= 2 &&
                    m.GetParameters()[0].ParameterType == typeof(Guid) &&
                    IsEnumerableOfGuid(m.GetParameters()[1].ParameterType));

            if (mi == null) return false;

            var pars = mi.GetParameters();
            var args = new List<object?> { collectionId, itemIds };
            for (int i = 2; i < pars.Length; i++)
            {
                var p = pars[i];
                if (p.ParameterType == typeof(CancellationToken)) args.Add(ct);
                else args.Add(GetDefault(p.ParameterType));
            }

            _log.LogInformation("[CBF] Add via {Sig}", FormatSignature(mi));

            try
            {
                await InvokeAsync(targetManager, mi, args.ToArray()).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "[CBF] AddToCollectionAsync fehlgeschlagen ({Sig})", FormatSignature(mi));
                return false;
            }
        }

        // ---------- Helpers ----------

        private static bool SetStringProperty(object obj, string name, string value)
        {
            var pi = obj.GetType().GetProperty(name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (pi != null && pi.PropertyType == typeof(string))
            {
                pi.SetValue(obj, value);
                return true;
            }
            return false;
        }

        private static bool TrySetGuidEnumerable(object obj, IEnumerable<string> candidates, IEnumerable<Guid> ids)
        {
            foreach (var n in candidates)
            {
                var pi = obj.GetType().GetProperty(n,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (pi == null) continue;

                if (IsEnumerableOfGuid(pi.PropertyType))
                {
                    object value = ids.ToList(); // List<Guid> safe default
                    pi.SetValue(obj, value);
                    return true;
                }
            }
            return false;
        }

        private static bool IsEnumerableOfGuid(Type t)
        {
            if (t == typeof(IEnumerable<Guid>)) return true;

            if (t.IsGenericType)
            {
                var def = t.GetGenericTypeDefinition();
                if (def == typeof(IEnumerable<>) ||
                    def == typeof(IReadOnlyCollection<>) ||
                    def == typeof(ICollection<>) ||
                    def == typeof(IList<>))
                {
                    return t.GetGenericArguments()[0] == typeof(Guid);
                }
            }
            return false;
        }

        private static object? GetDefault(Type t) =>
            t.IsValueType ? Activator.CreateInstance(t) : null;

        private static async Task<object?> InvokeAsync(object target, MethodInfo mi, object?[] args)
        {
            var ret = mi.Invoke(target, args);
            if (ret is Task task)
            {
                await task.ConfigureAwait(false);
                if (ret.GetType().IsGenericType)
                {
                    return ret.GetType().GetProperty("Result")?.GetValue(ret);
                }
                return null;
            }

            return ret;
        }

        private static string NormEndSlash(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            s = s.Trim();
            if (!s.EndsWith(Path.DirectorySeparatorChar) &&
                !s.EndsWith(Path.AltDirectorySeparatorChar))
            {
                s += Path.DirectorySeparatorChar;
            }
            return s;
        }

        private static string FormatSignature(MethodInfo mi)
        {
            var pars = string.Join(", ", mi.GetParameters().Select(p => p.ParameterType.Name));
            return $"{mi.Name}({pars})";
        }

        private void DumpAvailableApis()
        {
            try
            {
                DumpType("Instance", _collections.GetType());
                DumpType("Instance", _library.GetType());
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "[CBF-API] Dump error");
            }

            void DumpType(string kind, Type t)
            {
                foreach (var m in t.GetMethods(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (!m.Name.Contains("Collection", StringComparison.OrdinalIgnoreCase)) continue;
                    _log.LogWarning("[CBF-API] {Kind}: {Sig}", kind, FormatSignature(m));
                }
            }
        }
    }
}
