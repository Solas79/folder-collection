using System;
using System.Collections;
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
    /// <summary>
    /// Baut/aktualisiert Collections anhand der gespeicherten Plugin-Konfiguration.
    /// – 10.10-kompatibel (BaseItemKind)
    /// – Findet & nutzt Instanz- und Extension-Methoden auf ICollectionManager **und** ILibraryManager
    /// – Loggt alle gefundenen API-Signaturen, falls kein Treffer klappt.
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
                .Select(NormalizePathEndSlash)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var bl = (cfg.Blacklist ?? new List<string>())
                .Select(NormalizePathEndSlash)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var min = Math.Max(1, cfg.MinFiles);
            var prefix = cfg.Prefix ?? string.Empty;
            var suffix = cfg.Suffix ?? string.Empty;

            _log.LogInformation("[CBF] Scan start: WL={W} BL={B} Min={Min} Prefix='{P}' Suffix='{S}'",
                wl.Count, bl.Count, min, prefix, suffix);

            // 1) Movies laden (10.10: BaseItemKind)
            var movies = _library.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie },
                Recursive = true
            }).OfType<Movie>().ToList();

            _log.LogInformation("[CBF] Movies gesamt: {N}", movies.Count);

            // 2) WL/BL-Filter
            bool IsInWhitelist(string path) =>
                wl.Count == 0 || wl.Any(w => path.StartsWith(w, StringComparison.OrdinalIgnoreCase));
            bool IsInBlacklist(string path) =>
                bl.Any(b => path.StartsWith(b, StringComparison.OrdinalIgnoreCase));

            var filtered = movies.Where(m =>
            {
                var p = m.Path ?? string.Empty;
                return IsInWhitelist(p) && !IsInBlacklist(p);
            }).ToList();

            _log.LogInformation("[CBF] Movies nach WL/BL: {N}", filtered.Count);

            // 3) Nach Ordner gruppieren
            var groups = filtered
                .GroupBy(m =>
                {
                    var dir = Path.GetDirectoryName(m.Path ?? string.Empty) ?? string.Empty;
                    return dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                })
                .Where(g => g.Key.Length > 0)
                .ToList();

            _log.LogInformation("[CBF] Ordner-Gruppen gesamt: {N}", groups.Count);

            int created = 0, updated = 0;

            foreach (var g in groups)
            {
                ct.ThrowIfCancellationRequested();

                var dirPath = g.Key;
                var itemCount = g.Count();
                if (itemCount < min)
                {
                    _log.LogDebug("[CBF] Skip '{Dir}' (Count={C} < Min={Min})", dirPath, itemCount, min);
                    continue;
                }

                var folderName = Path.GetFileName(dirPath);
                if (string.IsNullOrWhiteSpace(folderName))
                {
                    _log.LogDebug("[CBF] Skip '{Dir}' (kein Ordnername)", dirPath);
                    continue;
                }

                var name = $"{prefix}{folderName}{suffix}";
                var items = g.Cast<BaseItem>().ToList();
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
                           await TryCreateCollection_OnCollectionManager(name, items, itemIds, ct).ConfigureAwait(false)
                        || await TryCreateCollection_OnLibraryManager   (name, items, itemIds, ct).ConfigureAwait(false);

                    if (!ok)
                    {
                        _log.LogWarning("[CBF] CreateCollection API nicht gefunden/fehlgeschlagen für '{Name}'", name);
                        DumpAvailableApis(); // <- diagnostik
                        continue;
                    }
                    created++;
                }
                else
                {
                    var exIds = new HashSet<Guid>(existing.GetLinkedChildren().Select(x => x.Id));
                    var toAdd = items.Where(x => !exIds.Contains(x.Id)).ToList();
                    var toAddIds = toAdd.Select(x => x.Id).ToList();

                    if (toAdd.Count > 0)
                    {
                        _log.LogInformation("[CBF] Update Collection '{Name}' (+{C} Items)", name, toAdd.Count);

                        var ok =
                               await TryAddToCollection_OnCollectionManager(existing, toAdd, toAddIds, ct).ConfigureAwait(false)
                            || await TryAddToCollection_OnLibraryManager   (existing, toAdd, toAddIds, ct).ConfigureAwait(false);

                        if (!ok)
                        {
                            _log.LogWarning("[CBF] AddToCollection API nicht gefunden/fehlgeschlagen für '{Name}'", name);
                            DumpAvailableApis(); // <- diagnostik
                            continue;
                        }
                        updated++;
                    }
                    else
                    {
                        _log.LogDebug("[CBF] Collection '{Name}' ist bereits aktuell.", name);
                    }
                }
            }

            _log.LogInformation("[CBF] Scan fertig: created={Cr} updated={Up}", created, updated);
            return (created, updated);
        }

        // =========================
        //  A) ICollectionManager
        // =========================

        private async Task<bool> TryCreateCollection_OnCollectionManager(
            string name, List<BaseItem> items, List<Guid> itemIds, CancellationToken ct)
        {
            // 1) Instanzmethoden
            if (await TryInvoke_Create_Instance(_collections, name, items, itemIds, ct).ConfigureAwait(false))
                return true;

            // 2) Extensionmethoden (static class, [Extension], 1. Param ICollectionManager)
            if (await TryInvoke_Create_Extension(typeof(ICollectionManager), _collections, name, items, itemIds, ct).ConfigureAwait(false))
                return true;

            return false;
        }

        private async Task<bool> TryAddToCollection_OnCollectionManager(
            BoxSet box, List<BaseItem> items, List<Guid> itemIds, CancellationToken ct)
        {
            if (await TryInvoke_Add_Instance(_collections, box, items, itemIds, ct).ConfigureAwait(false))
                return true;

            if (await TryInvoke_Add_Extension(typeof(ICollectionManager), _collections, box, items, itemIds, ct).ConfigureAwait(false))
                return true;

            return false;
        }

        // ======================
        //  B) ILibraryManager
        // ======================

        private async Task<bool> TryCreateCollection_OnLibraryManager(
            string name, List<BaseItem> items, List<Guid> itemIds, CancellationToken ct)
        {
            if (await TryInvoke_Create_Instance(_library, name, items, itemIds, ct).ConfigureAwait(false))
                return true;

            if (await TryInvoke_Create_Extension(typeof(ILibraryManager), _library, name, items, itemIds, ct).ConfigureAwait(false))
                return true;

            return false;
        }

        private async Task<bool> TryAddToCollection_OnLibraryManager(
            BoxSet box, List<BaseItem> items, List<Guid> itemIds, CancellationToken ct)
        {
            if (await TryInvoke_Add_Instance(_library, box, items, itemIds, ct).ConfigureAwait(false))
                return true;

            if (await TryInvoke_Add_Extension(typeof(ILibraryManager), _library, box, items, itemIds, ct).ConfigureAwait(false))
                return true;

            return false;
        }

        // =========================
        //  Gemeinsame Invoker
        // =========================

        private async Task<bool> TryInvoke_Create_Instance(
            object target, string name, List<BaseItem> items, List<Guid> ids, CancellationToken ct)
        {
            var methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                          .Where(m => m.Name.Contains("Create", StringComparison.OrdinalIgnoreCase) &&
                                      m.Name.Contains("Collection", StringComparison.OrdinalIgnoreCase))
                          .ToArray();

            foreach (var mi in methods)
            {
                var p = mi.GetParameters();
                if (p.Length < 2) continue;

                try
                {
                    object?[] args;

                    // (string, IEnumerable<BaseItem>/IReadOnlyCollection<BaseItem>/…)
                    if (p[0].ParameterType == typeof(string) && IsEnumerableOf(p[1].ParameterType, typeof(BaseItem)))
                    {
                        _log.LogInformation("[CBF] Create via {Sig}", FormatSignature(mi));
                        args = BuildArgsFrom2(mi, name, (IEnumerable<BaseItem>)items, ct);
                        await InvokeAsync(target, mi, args).ConfigureAwait(false);
                        return true;
                    }

                    // (string, IEnumerable<Guid>/IReadOnlyCollection<Guid>/…)
                    if (p[0].ParameterType == typeof(string) && IsEnumerableOf(p[1].ParameterType, typeof(Guid)))
                    {
                        _log.LogInformation("[CBF] Create via {Sig}", FormatSignature(mi));
                        args = BuildArgsFrom2(mi, name, (IEnumerable<Guid>)ids, ct);
                        await InvokeAsync(target, mi, args).ConfigureAwait(false);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _log.LogDebug(ex, "[CBF] Create (Instanz) via {M} fehlgeschlagen", mi.Name);
                }
            }
            return false;
        }

        private async Task<bool> TryInvoke_Add_Instance(
            object target, BoxSet box, List<BaseItem> items, List<Guid> ids, CancellationToken ct)
        {
            var methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                          .Where(m =>
                              m.Name.Contains("AddToCollection", StringComparison.OrdinalIgnoreCase) ||
                              m.Name.Contains("AddItemsToCollection", StringComparison.OrdinalIgnoreCase))
                          .ToArray();

            foreach (var mi in methods)
            {
                var p = mi.GetParameters();
                if (p.Length < 2) continue;

                try
                {
                    object?[] args;

                    // (BoxSet, IEnumerable<BaseItem>/…)
                    if (p[0].ParameterType.IsAssignableFrom(typeof(BoxSet)) &&
                        IsEnumerableOf(p[1].ParameterType, typeof(BaseItem)))
                    {
                        _log.LogInformation("[CBF] Add via {Sig}", FormatSignature(mi));
                        args = BuildArgsFrom2(mi, (object)box, (IEnumerable<BaseItem>)items, ct);
                        await InvokeAsync(target, mi, args).ConfigureAwait(false);
                        return true;
                    }

                    // (Guid, IEnumerable<Guid>/…)
                    if (p[0].ParameterType == typeof(Guid) &&
                        IsEnumerableOf(p[1].ParameterType, typeof(Guid)))
                    {
                        _log.LogInformation("[CBF] Add via {Sig}", FormatSignature(mi));
                        args = BuildArgsFrom2(mi, (object)box.Id, (IEnumerable<Guid>)ids, ct);
                        await InvokeAsync(target, mi, args).ConfigureAwait(false);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _log.LogDebug(ex, "[CBF] Add (Instanz) via {M} fehlgeschlagen", mi.Name);
                }
            }
            return false;
        }

        private async Task<bool> TryInvoke_Create_Extension(
            Type firstParamType, object target, string name, List<BaseItem> items, List<Guid> ids, CancellationToken ct)
        {
            foreach (var mi in FindExtensionMethods(m =>
                       m.Name.Contains("Create", StringComparison.OrdinalIgnoreCase) &&
                       m.Name.Contains("Collection", StringComparison.OrdinalIgnoreCase) &&
                       FirstParamIs(m, firstParamType)))
            {
                var p = mi.GetParameters();
                if (p.Length < 3) continue;

                try
                {
                    object?[] args;

                    // (manager, string, IEnumerable<BaseItem>/…)
                    if (p[1].ParameterType == typeof(string) && IsEnumerableOf(p[2].ParameterType, typeof(BaseItem)))
                    {
                        _log.LogInformation("[CBF] Create via ext {Sig}", FormatSignature(mi));
                        args = BuildArgsFrom3Ext(mi, target, name, (IEnumerable<BaseItem>)items, ct);
                        await InvokeStaticAsync(mi, args).ConfigureAwait(false);
                        return true;
                    }

                    // (manager, string, IEnumerable<Guid>/…)
                    if (p[1].ParameterType == typeof(string) && IsEnumerableOf(p[2].ParameterType, typeof(Guid)))
                    {
                        _log.LogInformation("[CBF] Create via ext {Sig}", FormatSignature(mi));
                        args = BuildArgsFrom3Ext(mi, target, name, (IEnumerable<Guid>)ids, ct);
                        await InvokeStaticAsync(mi, args).ConfigureAwait(false);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _log.LogDebug(ex, "[CBF] Create (ext) via {M} fehlgeschlagen", mi.Name);
                }
            }
            return false;
        }

        private async Task<bool> TryInvoke_Add_Extension(
            Type firstParamType, object target, BoxSet box, List<BaseItem> items, List<Guid> ids, CancellationToken ct)
        {
            foreach (var mi in FindExtensionMethods(m =>
                       (m.Name.Contains("AddToCollection", StringComparison.OrdinalIgnoreCase) ||
                        m.Name.Contains("AddItemsToCollection", StringComparison.OrdinalIgnoreCase)) &&
                       FirstParamIs(m, firstParamType)))
            {
                var p = mi.GetParameters();
                if (p.Length < 3) continue;

                try
                {
                    object?[] args;

                    // (manager, BoxSet, IEnumerable<BaseItem>/…)
                    if (p[1].ParameterType.IsAssignableFrom(typeof(BoxSet)) &&
                        IsEnumerableOf(p[2].ParameterType, typeof(BaseItem)))
                    {
                        _log.LogInformation("[CBF] Add via ext {Sig}", FormatSignature(mi));
                        args = BuildArgsFrom3Ext(mi, target, (object)box, (IEnumerable<BaseItem>)items, ct);
                        await InvokeStaticAsync(mi, args).ConfigureAwait(false);
                        return true;
                    }

                    // (manager, Guid, IEnumerable<Guid>/…)
                    if (p[1].ParameterType == typeof(Guid) &&
                        IsEnumerableOf(p[2].ParameterType, typeof(Guid)))
                    {
                        _log.LogInformation("[CBF] Add via ext {Sig}", FormatSignature(mi));
                        args = BuildArgsFrom3Ext(mi, target, (object)box.Id, (IEnumerable<Guid>)ids, ct);
                        await InvokeStaticAsync(mi, args).ConfigureAwait(false);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _log.LogDebug(ex, "[CBF] Add (ext) via {M} fehlgeschlagen", mi.Name);
                }
            }
            return false;
        }

        // =========================
        //  Reflection-Helfer
        // =========================

        private static IEnumerable<MethodInfo> FindExtensionMethods(Func<MethodInfo, bool> filter)
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

        private static bool FirstParamIs(MethodInfo mi, Type expected)
        {
            var p = mi.GetParameters();
            return p.Length > 0 && expected.IsAssignableFrom(p[0].ParameterType);
        }

        private static bool IsEnumerableOf(Type candidate, Type elem)
        {
            if (candidate == typeof(string)) return false;

            // Arrays
            if (candidate.IsArray)
            {
                var et = candidate.GetElementType();
                return et != null && (et == elem || elem.IsAssignableFrom(et) || et.IsAssignableFrom(elem));
            }

            IEnumerable<Type> toCheck = candidate.GetInterfaces().Concat(new[] { candidate });
            foreach (var it in toCheck)
            {
                if (!it.IsGenericType) continue;
                var def = it.GetGenericTypeDefinition();
                if (def == typeof(IEnumerable<>) ||
                    def == typeof(IReadOnlyCollection<>) ||
                    def == typeof(ICollection<>) ||
                    def == typeof(IList<>))
                {
                    var t = it.GetGenericArguments()[0];
                    if (t == elem || elem.IsAssignableFrom(t) || t.IsAssignableFrom(elem))
                        return true;
                }
            }
            return false;
        }

        private static object?[] BuildArgsFrom2(MethodInfo mi, object a0, object a1, CancellationToken ct)
        {
            var pars = mi.GetParameters();
            var args = new List<object?> { a0, a1 };
            for (int i = 2; i < pars.Length; i++)
            {
                var p = pars[i];
                if (p.ParameterType == typeof(CancellationToken)) { args.Add(ct); continue; }
                if (p.ParameterType == typeof(string)) { args.Add(null); continue; }
                if (p.ParameterType == typeof(Guid)) { args.Add(Guid.Empty); continue; }
                if (p.ParameterType == typeof(Guid?)) { args.Add((Guid?)null); continue; }
                args.Add(GetDefault(p.ParameterType));
            }
            return args.ToArray();
        }

        private static object?[] BuildArgsFrom3Ext(MethodInfo mi, object a0, object a1, object a2, CancellationToken ct)
        {
            var pars = mi.GetParameters();
            var args = new List<object?> { a0, a1, a2 };
            for (int i = 3; i < pars.Length; i++)
            {
                var p = pars[i];
                if (p.ParameterType == typeof(CancellationToken)) { args.Add(ct); continue; }
                if (p.ParameterType == typeof(string)) { args.Add(null); continue; }
                if (p.ParameterType == typeof(Guid)) { args.Add(Guid.Empty); continue; }
                if (p.ParameterType == typeof(Guid?)) { args.Add((Guid?)null); continue; }
                args.Add(GetDefault(p.ParameterType));
            }
            return args.ToArray();
        }

        private static object? GetDefault(Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;

        private static async Task<object?> InvokeAsync(object target, MethodInfo mi, object?[] args)
        {
            var ret = mi.Invoke(target, args);
            if (ret is Task task)
            {
                await task.ConfigureAwait(false);
                var type = ret.GetType();
                if (type.IsGenericType)
                {
                    var prop = type.GetProperty("Result");
                    return prop?.GetValue(ret);
                }
                return null;
            }
            return ret;
        }

        private static async Task<object?> InvokeStaticAsync(MethodInfo mi, object?[] args)
        {
            var ret = mi.Invoke(null, args);
            if (ret is Task task)
            {
                await task.ConfigureAwait(false);
                var type = ret.GetType();
                if (type.IsGenericType)
                {
                    var prop = type.GetProperty("Result");
                    return prop?.GetValue(ret);
                }
                return null;
            }
            return ret;
        }

        private static string NormalizePathEndSlash(string s)
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

        // ---- Diagnose: verfügbare API-Signaturen ausgeben ----
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

            void DumpExtensions(Type firstParam)
            {
                foreach (var mi in FindExtensionMethods(m =>
                           m.Name.Contains("Collection", StringComparison.OrdinalIgnoreCase) &&
                           FirstParamIs(m, firstParam)))
                {
                    _log.LogWarning("[CBF-API] Extension: {Sig}", FormatSignature(mi));
                }
            }
        }
    }
}
