# CollectionsByFolder (Jellyfin Plugin)

Erstellt und pflegt Sammlungen (Collections) basierend auf dem **letzten Ordnernamen**.

## Features
- Mehrere Bibliotheks-Wurzelpfade
- Präfix & Suffix für Collection-Namen
- Blacklist für Ordnernamen
- Mindestanzahl an Elementen pro Ordner
- Geplanter täglicher Scan (Uhrzeit + Checkbox)
- Button **Jetzt scannen** in den Einstellungen

## Build
```bash
dotnet publish src/Jellyfin.Plugin.CollectionsByFolder/Jellyfin.Plugin.CollectionsByFolder.csproj -c Release
```

DLL danach nach `plugins/CollectionsByFolder/` kopieren und Jellyfin neu starten.
