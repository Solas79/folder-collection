# CollectionsByFolder (Jellyfin Plugin)

Erstellt und pflegt Sammlungen (Collections) basierend auf dem **letzten Ordnernamen**.

## Features
- Mehrere Verzeichnisse möglich
- Präfix & Suffix für Collection-Namen
- Blacklist für Ordnernamen
- Mindestanzahl an Elementen pro Ordner
- Button **Jetzt scannen** in den Einstellungen

Es werden nur Ordner aus der Whitelist gescannt
Ordner in der Blacklist werden ignoriert

Es werden alle Unterordner des angegebenen Verzeichnisses gescannt und  nur die "Endordner" als Collection angelegt.

In der WHitelist und Blacklist sind die Ordnerstrukturen so einzutragen
Beispiel:
/mnt/nas1/Filme
(Linux: gemountetes Laufwerk:nas1 / Ordner Filme)

Beispeil Docker:
/data/Filme
/media/Filme

<img width="401" height="123" alt="Bildschirmfoto 2025-10-07 um 10 19 39" src="https://github.com/user-attachments/assets/80f820fa-2118-464d-8fb9-ef1511a8dd79" />

Präfix -> erstellt einen Eintrag vor dem Collection Namen
Sufix -> erstellt einen Eintrag nach dem Collection Namen

Mindestanzahl an Dateien -> Ordner mit weniger Dateien werden ignoriert und nicht als Collection angelegt.

Wurde mit **Jellyfin 10.10.7** (Linuxserver) getestet



## Build
```bash
dotnet publish src/Jellyfin.Plugin.CollectionsByFolder/Jellyfin.Plugin.CollectionsByFolder.csproj -c Release
```

DLL danach nach `plugins/CollectionsByFolder/` kopieren und Jellyfin neu starten.
