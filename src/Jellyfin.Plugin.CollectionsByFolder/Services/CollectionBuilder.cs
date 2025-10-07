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
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionsByFolder.Services
{
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
                .Select(NormEndSlash).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var bl = (cfg.Blacklist ?? new List<string>())
                .Select(NormEndSlash).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var min    = Math.Max(1, cfg.MinFiles);
            var prefix = cfg.Prefix ?? string.Empty;
            var suffix = cfg.Suffix ?? string.Empty;

            _log.LogInformation("[CBF] Scan start: WL={W} BL={B} Min={Min} Prefix='{P}' Suffix='{S}'",
                wl.Count, bl.Count, min, prefix, suffix);

            var movies = _library.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie },
                Recursive = true
            }).OfType<Movie>().ToList();

            _log.LogInformation("[CBF] Movies gesamt: {N}", movies.Count);

            bool WL(string p) => wl.Count == 0 || wl.Any(w => p.StartsWith(w, StringComparison.OrdinalIgnoreCase));
            bool BL(string p) => bl.Any(b => p.StartsWith(b, StringComparison.OrdinalIgnoreCase));

            var filtered = movies.Where(m => { var p = m.Path ?? ""; return WL(p) && !BL(p); }).ToList();
            _log.LogInformation("[CBF] Movies nach WL/BL: {N}", filtered.Count);

            var groups = filtered
                .GroupBy(m => (Path.GetDirectoryName(m.Path ?? "") ?? "")
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                .Where(g => g.Key.Length > 0)
                .ToList();

            _log.LogInformation("[CBF] Ordner-Gruppen gesamt: {N}", groups.Count);

            int created = 0, updated = 0;

            foreach (var g in groups)
            {
                ct.ThrowIfCancellationRequested();

                if (g.Count() < min) continue;

                var folderName = Path.GetFileName(g.Key);
                if (string.IsNullOrWhiteSpace(folderName)) continue;

                var name    = $"{prefix}{folderName}{suffix}";
                var items   = g.Cast<BaseItem>().ToList();
                var itemIds = items.Select(i => i.Id).ToList();

                // BoxSet existiert?
                var existing = _library.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.BoxSet },
                    Name = name
                }).OfType<BoxSet>().FirstOrDefault();

                if (existing == null)
                {
                    _log.LogInformation("[CBF] Create Collection '{Name}' (Items={C})", name, items.Count);

                    var ok =
                           await TryCreateCollection_OptionsAsync(_collections, name, itemIds, ct).ConfigureAwait(false)
                        || await TryCreateCollection_OptionsAsync(_library,     name, itemIds, ct).ConfigureAwait(false);

                    if (!ok)
                    {
                        _log.LogWarning("[CBF] CreateCollection API nicht gefunden/fehlgeschlagen für '{Name}'", name);
                        DumpAvailableApis();
                        continue;
                    }

                    created++;
                }
                else
                {
                    var ex = new HashSet<Guid>(existing.GetLinkedChildren().Select(x => x.Id));
                    var toAdd   = items.Where(x => !ex.Contains(x.Id)).ToList();
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

        // ---------- Create: CreateCollectionAsync(CollectionCreationOptions [, CancellationToken]) ----------

        private async Task<bool> TryCreateCollection_OptionsAsync(
            object targetManager, string name, IReadOnlyCollection<Guid> itemIds, CancellationToken ct)
        {
            // Passende Methode suchen
            var mi = targetManager.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m =>
                    m.Name.IndexOf("CreateCollectionAsync", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    m.GetParameters().Length >= 1 &&
                    m.GetParameters()[0].ParameterType.Name.IndexOf("CollectionCreationOptions",
                        StringComparison.OrdinalIgnoreCase) >= 0);

            if (mi == null) return false;

            var pars = mi.GetParameters();
            var optionsType = pars[0].ParameterType;

            // Options-Objekt bauen
            var options = Activator.CreateInstance(optionsType);
            if (options == null) return false;

            // Name setzen (Property "Name" oder "CollectionName")
            SetStringProperty(options, "Name", name) ||
            SetStringProperty(options, "CollectionName", name);

            // Item-IDs in ein Property mit IEnumerable<Guid> kippen (Ids / ItemIds / Items / MediaIds …)
            if (!TrySetGuidEnumerable(options, new[] { "ItemIds", "Ids", "Items", "MediaIds" }, itemIds))
            {
                // nicht schlimm: dann fügen wir unten separat per AddToCollection hinzu
                _log.LogDebug("[CBF] Options: keine Guid-Enumerable-Property gefunden – erzeuge leere Collection, füge später Items hinzu.");
            }

            // Argumente zusammensetzen
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

        // ---------- Add: AddToCollectionAsync(Guid, IEnumerable<Guid> [, CancellationToken]) ----------

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
            var pi = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (pi != null && (pi.PropertyType == typeof(string) || pi.PropertyType == typeof(string)))
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
                var pi = obj.GetType().GetProperty(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (pi == null) continue;

                if (IsEnumerableOfGuid(pi.PropertyType))
                {
                    // Falls Zieltyp kein IEnumerable<Guid> direkt ist (z. B. List<Guid>), konvertieren
                    object value = ids;
                    if (pi.PropertyType.IsGenericType &&
                        pi.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        var list = (IList<Guid>)Activator.CreateInstance(typeof(List<Guid>))!;
                        foreach (var g in ids) list.Add(g);
                        value = list;
                    }
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

        private static object? GetDefault(Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;

        private static async Task<object?> InvokeAsync(object target, MethodInfo mi, object?[] args)
        {
            var ret = mi.Invoke(target, args);
            if (ret is Task task)
            {
                await task.ConfigureAwait(false);
                if (ret.GetType().IsGenericType)
                    return ret.GetType().GetProperty("Result")?.GetValue(ret);
                return null;
            }
            return ret;
        }

        private static string NormEndSlash(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            s = s.Trim();
            if (!s.EndsWith(Path.DirectorySeparatorChar) && !s.EndsWith(Path.AltDirectorySeparatorChar))
                s += Path.DirectorySeparatorChar;
            return s;
        }

        private static string FormatSignature(MethodInfo mi)
        {
            var pars = string.Join(", ", mi.GetParameters().Select(p => p.ParameterType.Name));
            return $"{mi.Name}({pars})";
        }

        // ---- Diagnose: verfügbare API-Signaturen ausgeben (bleibt drin) ----
        private void DumpAvailableApis()
        {
            try
            {
                DumpType("Instance", _collections.GetType());
                DumpType("Instance", _library.GetType());
                DumpExtensions(typeof(ICollectionManager));
                DumpExtensions(typeof(ILibraryManager));
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

            static IEnumerable<MethodInfo> FindExtensionMethods(Func<MethodInfo, bool> filter)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type[] types;
                    try { types = asm.GetTypes(); }
                    catch { continue; }

                    foreach (var t in types)
                    {
                        if (!t.IsSealed || !t.IsAbstract) continue; // static class
                        var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static);
                        foreach (var m in methods)
                        {
                            if (!m.IsDefined(typeof(ExtensionAttribute), inherit: false)) continue;
                            if (filter(m)) yield return m;
                        }
                    }
                }
            }

            void DumpExtensions(Type firstParam)
            {
                foreach (var mi in FindExtensionMethods(m =>
                           m.Name.Contains("Collection", StringComparison.OrdinalIgnoreCase) &&
                           miHasFirstParam(m, firstParam)))
                {
                    _log.LogWarning("[CBF-API] Extension: {Sig}", FormatSignature(mi));
                }

                static bool miHasFirstParam(MethodInfo mi, Type expected)
                {
                    var p = mi.GetParameters();
                    return p.Length > 0 && expected.IsAssignableFrom(p[0].ParameterType);
                }
            }
        }
    }
}
