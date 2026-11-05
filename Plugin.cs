using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.StreamJelly.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.StreamJelly;

/// <summary>
/// StreamJelly plugin: shows what the server actually streams to clients during active playback.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static Plugin? Instance { get; private set; }

    public override string Name => "StreamJelly";

    public override Guid Id => Guid.Parse("7cbb0962-443d-4821-92f1-86085e90ded5");

    public override string Description => "Shows what the server really streams to each client: direct play, remux or transcode, per stream, with live ffmpeg details.";

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "streamjelly",
                DisplayName = "StreamJelly",
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Web.streamjelly.html", GetType().Namespace),
                EnableInMainMenu = true,
                MenuSection = "server",
                MenuIcon = "insights"
            }
        };
    }
}
