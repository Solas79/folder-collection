using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    /// Kompatibel zu Jellyfin 10.10.x (keine DtoOptions, IncludeItemTypes als BaseItemKind[]).
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

            // 1) Alle Movies holen (Entities reichen, wir brauchen Id/Path/Name)
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

            // 3) Nach Parent-Ordner gruppieren
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

                // 4) BoxSet mit Name finden
                var existing = _library.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.BoxSet },
                    Name = name
                }).OfType<BoxSet>().FirstOrDefault();

                if (existing == null)
                {
                    _log.LogInformation("[CBF] Create Collection '{Name}' (Items={C})", name, items.Count);
                    var createdOk = await TryCreateCollectionAsync(name, items, itemIds, ct).ConfigureAwait(false);
                    if (!createdOk)
                    {
                        _log.LogWarning("[CBF] CreateCollection API nicht gefunden/fehlgeschlagen für '{Name}'", name);
                        continue;
                    }
                    created++;
                }
                else
                {
                    // Fehlen Items?
                    var exIds = new HashSet<Guid>(existing.GetLinkedChildren().Select(x => x.Id));
                    var toAdd = items.Where(x => !exIds.Contains(x.Id)).ToList();
                    var toAddIds = toAdd.Select(x => x.Id).ToList();

                    if (toAdd.Count > 0)
                    {
                        _log.LogInformation("[CBF] Update Collection '{Name}' (+{C} Items)", name, toAdd.Count);
                        var ok = await TryAddToCollectionAsync(existing, toAdd, toAddIds, ct).ConfigureAwait(false);
                        if (!ok)
                        {
                            _log.LogWarning("[CBF] AddToCollection API nicht gefunden/fehlgeschlagen für '{Name}'", name);
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

        // ---------- Reflection-Helfer (robust für 10.10.x) ----------

        private async Task<bool> TryCreateCollectionAsync(
            string name,
            List<BaseItem> items,
            List<Guid> itemIds,
            CancellationToken ct)
        {
            var t = _collections.GetType();
            var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                           .Where(m => m.Name.IndexOf("Create", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                       m.Name.IndexOf("Collection", StringComparison.OrdinalIgnoreCase) >= 0)
                           .ToArray();

            foreach (var mi in methods)
            {
                var pars = mi.GetParameters();
                if (pars.Length < 2) continue;
                if (pars[0].ParameterType != typeof(string)) continue;

                try
                {
                    object?[] args;
                    if (IsEnumerableOf(pars[1].ParameterType, typeof(BaseItem)))
                    {
                        args = BuildArgs(mi, name, (IEnumerable<BaseItem>)items, ct);
                    }
                    else if (IsEnumerableOf(pars[1].ParameterType, typeof(Guid)))
                    {
                        args = BuildArgs(mi, name, (IEnumerable<Guid>)itemIds, ct);
                    }
                    else
                    {
                        continue;
                    }

                    _log.LogInformation("[CBF] Create via {Sig}", FormatSignature(mi));
                    await InvokeAsync(_collections, mi, args).ConfigureAwait(false);
                    return true;
                }
                catch (Exception ex)
                {
                    _log.LogDebug(ex, "[CBF] CreateCollection via {M} fehlgeschlagen", mi.Name);
                }
            }

            return false;
        }

        private async Task<bool> TryAddToCollectionAsync(
            BoxSet box,
            List<BaseItem> items,
            List<Guid> itemIds,
            CancellationToken ct)
        {
            var t = _collections.GetType();
            var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                           .Where(m =>
                               m.Name.IndexOf("AddToCollection", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               m.Name.IndexOf("AddItemsToCollection", StringComparison.OrdinalIgnoreCase) >= 0)
                           .ToArray();

            foreach (var mi in methods)
            {
                var pars = mi.GetParameters();
                if (pars.Length < 2) continue;

                try
                {
                    object?[] args;

                    // (BoxSet, IEnumerable<BaseItem> / IReadOnlyCollection<BaseItem> / …)
                    if (pars[0].ParameterType.IsAssignableFrom(typeof(BoxSet)) &&
                        IsEnumerableOf(pars[1].ParameterType, typeof(BaseItem)))
                    {
                        args = BuildArgs(mi, (object)box, (IEnumerable<BaseItem>)items, ct);
                    }
                    // (Guid, IEnumerable<Guid> / IReadOnlyCollection<Guid> / …)
                    else if (pars[0].ParameterType == typeof(Guid) &&
                             IsEnumerableOf(pars[1].ParameterType, typeof(Guid)))
                    {
                        args = BuildArgs(mi, (object)box.Id, (IEnumerable<Guid>)itemIds, ct);
                    }
                    else
                    {
                        continue;
                    }

                    _log.LogInformation("[CBF] Add via {Sig}", FormatSignature(mi));
                    await InvokeAsync(_collections, mi, args).ConfigureAwait(false);
                    return true;
                }
                catch (Exception ex)
                {
                    _log.LogDebug(ex, "[CBF] AddToCollection via {M} fehlgeschlagen", mi.Name);
                }
            }

            return false;
        }

        // Prüft „ist Aufzählung von elem?“ – akzeptiert Arrays, IList<>, IReadOnlyCollection<>, IEnumerable<>, usw.
        private static bool IsEnumerableOf(Type candidate, Type elem)
        {
            if (candidate == typeof(string)) return false;

            // Arrays
            if (candidate.IsArray)
            {
                var et = candidate.GetElementType();
                return et != null && (et == elem || elem.IsAssignableFrom(et) || et.IsAssignableFrom(elem));
            }

            // Alle Interfaces + der Typ selbst auf IEnumerable<T> prüfen
            IEnumerable<Type> toCheck = candidate.IsInterface
                ? candidate.GetInterfaces().Concat(new[] { candidate })
                : candidate.GetInterfaces().Concat(new[] { candidate });

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

        private static object?[] BuildArgs(MethodInfo mi, object arg0, object arg1, CancellationToken ct)
        {
            var pars = mi.GetParameters();
            var args = new List<object?> { arg0, arg1 };

            for (int i = 2; i < pars.Length; i++)
            {
                var p = pars[i];

                if (p.ParameterType == typeof(CancellationToken))
                {
                    args.Add(ct);
                    continue;
                }

                // string? collectionFolderPath / parentId o.ä. → null ist okay
                if (p.ParameterType == typeof(string))
                {
                    args.Add(null);
                    continue;
                }

                // Guid? ownerId / userId o.ä. → Default
                if (p.ParameterType == typeof(Guid) || p.ParameterType == typeof(Guid?))
                {
                    args.Add(p.ParameterType == typeof(Guid) ? Guid.Empty : (Guid?)null);
                    continue;
                }

                // Fallback: default(T)
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
    }
}
