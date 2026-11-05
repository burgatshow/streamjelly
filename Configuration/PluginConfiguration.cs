using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.StreamJelly.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Gets or sets the UI auto-refresh interval in seconds.</summary>
    public int RefreshSeconds { get; set; } = 3;

    /// <summary>Gets or sets a value indicating whether file system paths are returned by the API.</summary>
    public bool ShowPaths { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the full ffmpeg command line is returned.</summary>
    public bool ShowFfmpegCommand { get; set; } = true;
}
