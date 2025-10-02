*** a/src/FolderCollectionsTask.cs
--- b/src/FolderCollectionsTask.cs
@@
 using System;
 using System.Collections.Generic;
 using System.IO;
 using System.Threading;
 using System.Threading.Tasks;
 using MediaBrowser.Model.Tasks;
 using Microsoft.Extensions.Logging;
 
 namespace FolderCollections
 {
     public class FolderCollectionsTask : IScheduledTask
     {
         private readonly ILogger<FolderCollectionsTask> _logger;
 
         public FolderCollectionsTask(ILogger<FolderCollectionsTask> logger)
         {
             _logger = logger;
         }
 
         public string Name => "Folder Collections: täglicher Scan";
         public string Key => "FolderCollections.DailyScan";
         public string Description => "Erstellt/aktualisiert Sammlungen basierend auf der Ordnerstruktur.";
         public string Category => "Library";
 
         public async Task ExecuteAsync(IProgress<double>? progress, CancellationToken cancellationToken)
         {
             var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();
 
             _logger.LogInformation(
                 "FolderCollectionsTask gestartet. IncludeMovies={IncludeMovies}, IncludeSeries={IncludeSeries}, MinItems={MinItems}, Prefix='{Prefix}', Suffix='{Suffix}', Scan={Hour:D2}:{Minute:D2}{UseBaseName}",
-                cfg.IncludeMovies, cfg.IncludeSeries, cfg.MinItems, cfg.Prefix, cfg.Suffix, cfg.ScanHour, cfg.ScanMinute);
+                cfg.IncludeMovies, cfg.IncludeSeries, cfg.MinItems, cfg.Prefix, cfg.Suffix, cfg.ScanHour, cfg.ScanMinute,
+                cfg.UseBasenameAsCollectionName ? ", UseBasenameAsCollectionName=true" : ", UseBasenameAsCollectionName=false");
 
             progress?.Report(0);
 
-            // TODO: hier deine eigentliche Scan-/Erstell-Logik
-            await Task.Delay(200, cancellationToken);
+            // *** Scan-Logik ***
+            // Wir laufen über alle konfigurierten Wurzelpfade und sammeln Kandidatenverzeichnisse.
+            // (Hier sehr simpel: jedes Unterverzeichnis ist eine potentielle Sammlung.)
+            var roots = cfg.PathPrefixes ?? Array.Empty<string>();
+            var allDirs = new List<string>();
+
+            foreach (var root in roots)
+            {
+                if (string.IsNullOrWhiteSpace(root)) continue;
+                try
+                {
+                    if (Directory.Exists(root))
+                    {
+                        CollectDirectoriesRecursive(root, allDirs, cancellationToken);
+                    }
+                    else
+                    {
+                        _logger.LogWarning("Konfigurierter Pfad existiert nicht: {Root}", root);
+                    }
+                }
+                catch (Exception ex)
+                {
+                    _logger.LogError(ex, "Fehler beim Durchlaufen von Root {Root}", root);
+                }
+            }
+
+            // Fortschritt grob in 2 Teile: Scannen (50%) + Erstellen/Aktualisieren (50%)
+            progress?.Report(50);
+
+            // *** Erstellung/Aktualisierung ***
+            // Hier würdest du anhand IncludeMovies/IncludeSeries + MinItems filtern
+            // und dann eine Sammlung erzeugen/aktualisieren.
+            // In diesem Patch loggen wir erstmal nur den berechneten Namen.
+            var i = 0;
+            foreach (var dir in allDirs)
+            {
+                cancellationToken.ThrowIfCancellationRequested();
+
+                var collectionName = BuildCollectionName(dir, cfg);
+                _logger.LogInformation("Kandidat: Pfad='{Path}'  =>  Sammlungsname='{Name}'", dir, collectionName);
+
+                // TODO: Hier Sammlung erzeugen/aktualisieren.
+                // Beispiel (abhängig von deinem bereits verwendeten Service):
+                // await _collectionManager.CreateOrUpdateAsync(dir, collectionName, cfg, cancellationToken);
+
+                i++;
+                if (i % 50 == 0) // nicht zu häufig reporten
+                {
+                    // 50..95% linear hochzählen
+                    var pct = 50 + Math.Min(45, (int)Math.Round(45.0 * i / Math.Max(1, allDirs.Count)));
+                    progress?.Report(pct);
+                }
+            }
 
             progress?.Report(100);
             _logger.LogInformation("FolderCollectionsTask beendet.");
         }
 
         public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
         {
             // Standardmäßig keine Auto-Trigger setzen; Zeitplan im Dashboard konfigurieren
             return Array.Empty<TaskTriggerInfo>();
         }
+
+        private static void CollectDirectoriesRecursive(string root, List<string> bag, CancellationToken ct)
+        {
+            ct.ThrowIfCancellationRequested();
+            try
+            {
+                // Root selbst nicht als Sammlung nehmen? -> hier entscheiden.
+                // Wenn ja: bag.Add(root);
+
+                var subs = Directory.GetDirectories(root);
+                foreach (var sub in subs)
+                {
+                    ct.ThrowIfCancellationRequested();
+                    bag.Add(sub);
+                    CollectDirectoriesRecursive(sub, bag, ct);
+                }
+            }
+            catch (UnauthorizedAccessException)
+            {
+                // Überspringen, aber nicht crashen
+            }
+        }
+
+        private static string BuildCollectionName(string directoryPath, PluginConfiguration cfg)
+        {
+            if (string.IsNullOrWhiteSpace(directoryPath)) return string.Empty;
+
+            var trimmed = Path.TrimEndingDirectorySeparator(directoryPath);
+            var baseName = Path.GetFileName(trimmed);
+
+            var name = cfg.UseBasenameAsCollectionName
+                ? (string.IsNullOrEmpty(baseName) ? trimmed : baseName)
+                : trimmed;
+
+            if (!string.IsNullOrWhiteSpace(cfg.Prefix))
+                name = $"{cfg.Prefix}{name}";
+            if (!string.IsNullOrWhiteSpace(cfg.Suffix))
+                name = $"{name}{cfg.Suffix}";
+
+            return name;
+        }
     }
 }
