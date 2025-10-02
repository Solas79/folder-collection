using System;
using MediaBrowser.Model.Plugins;

namespace FolderCollections
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Filme in Sammlungen aufnehmen.
        /// </summary>
        public bool IncludeMovies { get; set; } = true;

        /// <summary>
        /// Serien (als Serie, nicht Episoden) in Sammlungen aufnehmen.
        /// </summary>
        public bool IncludeSeries { get; set; } = true;

        /// <summary>
        /// Mindestanzahl an Items (nach Filter), damit eine Sammlung erzeugt/aktualisiert wird.
        /// </summary>
        public int MinItems { get; set; } = 2;

        /// <summary>
        /// Optionaler Prefix für den Sammlungsnamen.
        /// </summary>
        public string? Prefix { get; set; }

        /// <summary>
        /// Optionaler Suffix für den Sammlungsnamen.
        /// </summary>
        public string? Suffix { get; set; }

        /// <summary>
        /// Wenn true, wird der letzte Ordnername (Basename) als Sammlungsname verwendet,
        /// andernfalls der komplette Pfadname.
        /// </summary>
        public bool UseBasenameAsCollectionName { get; set; } = true;

        /// <summary>
        /// Geplante Uhrzeit (Info; tatsächlicher Trigger übers Dashboard).
        /// </summary>
        public int ScanHour { get; set; } = 4;

        /// <summary>
        /// Geplante Minute (Info; tatsächlicher Trigger übers Dashboard).
        /// </summary>
        public int ScanMinute { get; set; } = 0;

        /// <summary>
        /// Wurzelpfade, unter denen gesucht wird (rekursiv).
        /// </summary>
        public string[] PathPrefixes { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Muster zum Ignorieren von Ordnern. Unterstützt:
        /// - Wildcards (* und ?)
        /// - Regex, wenn das Muster mit "re:" beginnt.
        /// </summary>
        public string[] IgnorePatterns { get; set; } = Array.Empty<string>();
    }
}
