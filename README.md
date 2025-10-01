# Folder Collections (GUI) – Jellyfin Plugin

Erzeugt/aktualisiert **Collections (BoxSets)** automatisch pro **Eltern-Ordner**.

**Features**
- Movies/Series wählbar
- Pfad-Präfixe (Whitelist), Ignore-Patterns (Regex)
- Mindestanzahl Items pro Ordner
- Namensschema (Basename, Prefix/Suffix)
- Geplante Aufgabe täglich 04:00; manuell ausführbar
- **GUI-Konfiguration** im Jellyfin-Dashboard

- Basename als Sammlungsname: Der **letzte Ordnernamen** wird als Sammlungsname verwendet, sonst **kompletter Pfadname**
- Prefix: wird dem Sammlungsnamen vorangesetzt (z.B. Box: -> Box: Harry Potter)
- Suffix: wird dem Sammlungsnamen angefügt (z.B. Collection -> Collection -> Harry Potter Collection)
- Ignore: Schließt alles aus, was im Pfad sample enthält. (z. B. /mnt/media/Filme/HarryPotter/sample/ → wird ignoriert)
- Pfad Präfixe: entspricht einer Art Whitelist. Es Werden nur Daten aus dem angegebenen Verzeichnis gescannt. Alle anderen werden Ignoriert. (z.B. /mnt/Media/File/HarryPotter scannt nur diesen Pfad)

## Installation (nur über GitHub)
1. **Manifest als Repository** in Jellyfin hinzufügen:
- *Dashboard → Plugins → Repositories → Add* – URL: https://raw.githubusercontent.com/Solas79/folder-collection/main/manifest.json
2. *Dashboard → Plugins → Catalog* → **Folder Collections** installieren.
3. Server neu starten.
4. *Dashboard → Plugins → Folder Collections (Zahnrad)* → konfigurieren.
5. *Dashboard → Scheduled Tasks* → **Folder Collections (per directory)** → „Run now“.

## Build/Release (automatisch über GitHub Actions)
- Tag pushen: `git tag v0.1.0 && git push --tags`
- Action baut, veröffentlicht ZIP & aktualisiert `manifest.json` (im Release), sodass Jellyfin das Paket findet.

## Hinweise
- Testet mit Jellyfin 10.10.x / .NET 8.
- Bei Fragen: Issues öffnen.
