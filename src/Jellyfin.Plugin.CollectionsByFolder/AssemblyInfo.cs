using System.Reflection;

[assembly: AssemblyVersion("0.1.0.0")]
[assembly: AssemblyFileVersion("0.1.0.0")]
```

---

## src/Jellyfin.Plugin.CollectionsByFolder/Plugin.cs
```csharp
using System;
using System.Collections.Generic;
using Jellyfin.Plugin.CollectionsByFolder.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CollectionsByFolder;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
public override string Name => "CollectionsByFolder";
public override Guid Id => Guid.Parse("f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41");

public static Plugin Instance { get; private set; } = null!;

public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
: base(applicationPaths, xmlSerializer)
{
Instance = this;
}

public IEnumerable<PluginPageInfo> GetPages()
{
return new[]
{
new PluginPageInfo
{
Name = "collectionsbyfolder",
EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
},
new PluginPageInfo
{
Name = "collectionsbyfolderjs",
EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.js"
}
};
}
}
