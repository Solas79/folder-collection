using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.FolderCollections.GUI;

public static class DirectoryGrouper
{
public static bool AcceptPath(string? path, IReadOnlyList<string> prefixes, IReadOnlyList<Regex> ignore)
{
if (string.IsNullOrWhiteSpace(path)) return false;
if (prefixes.Count > 0 && !prefixes.Any(p => path!.StartsWith(p, StringComparison.OrdinalIgnoreCase))) return false;
return !ignore.Any(rx => rx.IsMatch(path!));
}

public static string ParentFolder(string path)
{
var p = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
return Path.GetDirectoryName(p) ?? string.Empty;
}

public static string CollectionName(string folderPath, bool useBasename, string prefix, string suffix)
{
var core = useBasename ? Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) : folderPath;
return $"{prefix}{core}{suffix}";
}
}
