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
# .sln im Root vorhanden – alternativ direkt das csproj publishen
dotnet publish src/Jellyfin.Plugin.CollectionsByFolder/Jellyfin.Plugin.CollectionsByFolder.csproj -c Release
```
Das resultierende `Jellyfin.Plugin.CollectionsByFolder.dll` nach `plugins/CollectionsByFolder/` kopieren und den Server neu starten.

## Hinweise
- Passe die `targetAbi`/Paketversionen an deine Jellyfin-Serverversion an.
- Der Task-Trigger liest die Uhrzeit aus den Plugin-Einstellungen. Wenn du die Uhrzeit änderst, kann es nötig sein, den geplanten Task einmal **deaktivieren/aktivieren** (oder den Server neustarten), damit der Trigger neu registriert wird.
