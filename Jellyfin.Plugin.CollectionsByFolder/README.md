# CollectionsByFolder (Jellyfin Plugin)

Erstellt und pflegt Sammlungen (Collections) basierend auf dem **letzten Ordnernamen**.

## Features
- Mehrere Bibliotheks-Wurzelpfade (nur Medien unterhalb dieser Pfade werden berücksichtigt)
- Präfix & Suffix für den Collection-Namen
- Blacklist für Ordnernamen
- Mindestanzahl an Elementen pro Ordner
- Geplanter täglicher Scan (konfigurierbare Uhrzeit, aktivierbar per Checkbox)
- Button **Jetzt scannen** auf der Einstellungsseite

## Build
```bash
dotnet publish src/Jellyfin.Plugin.CollectionsByFolder/Jellyfin.Plugin.CollectionsByFolder.csproj -c Release
```
Das resultierende `Jellyfin.Plugin.CollectionsByFolder.dll` nach `plugins/CollectionsByFolder/` kopieren und den Server neu starten.

## Hinweise
- Passe die `targetAbi`/Paketversionen an deine Jellyfin-Serverversion an.
- Wenn du die Uhrzeit änderst, ggf. den geplanten Task einmal neu initialisieren (Server-Neustart oder Task togglen).
