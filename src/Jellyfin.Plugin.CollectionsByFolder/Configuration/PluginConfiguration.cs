using System;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CollectionsByFolder.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
public List<string> LibraryRoots { get; set; } = new();
public string Prefix { get; set; } = string.Empty;
public string Suffix { get; set; } = string.Empty;
public List<string> Blacklist { get; set; } = new();
public int MinItemsPerFolder { get; set; } = 2;

public bool DailyScanEnabled { get; set; } = true;
// 24h-Lokale Zeit, z. B. "03:30" => 3:30 Uhr
public string DailyScanTime { get; set; } = "03:30";
}
