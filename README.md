++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

**German version -> English version below**

++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
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

In der Whitelist und Blacklist sind die Ordnerstrukturen so einzutragen
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


++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

**English verision**

++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
# CollectionsByFolder (Jellyfin Plugin)

Creates and maintains collections based on the **last folder name**.

## Features
- Multiple directories supported
- Prefix & suffix for collection names
- Blacklist for folder names
- Minimum number of items per folder
- **Scan now** button in the settings

Only whitelisted folders are scanned
Folders in the blacklist are ignored

All subfolders of the specified directory are scanned, and only the "end folders" are created as collections.

The folder structures in the whitelist and blacklist should be entered as follows:
Example:
/mnt/nas1/Filme
(Linux: mounted drive: nas1 / folder "Filme")

Docker example:
/data/Filme
/media/Filme

<img width="401" height="123" alt="Screenshot 2025-10-07 at 10:19:39" src="https://github.com/user-attachments/assets/80f820fa-2118-464d-8fb9-ef1511a8dd79" />

Prefix -> creates an entry before the collection name
Sufix -> creates an entry after the collection name

Minimum number of files -> Folders with fewer files are ignored and not created as a collection.

Tested with **Jellyfin 10.10.7** (Linux server)


## Build
```bash
dotnet publish src/Jellyfin.Plugin.CollectionsByFolder/Jellyfin.Plugin.CollectionsByFolder.csproj -c Release
```

DLL danach nach `plugins/CollectionsByFolder/` kopieren und Jellyfin neu starten.
