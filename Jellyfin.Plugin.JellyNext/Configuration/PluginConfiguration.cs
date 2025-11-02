using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.JellyNext.Configuration;

/// <summary>
/// Plugin configuration for JellyNext.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the Trakt Client ID.
    /// </summary>
    public string TraktClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Trakt Client Secret.
    /// </summary>
    public string TraktClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Radarr URL.
    /// </summary>
    public string RadarrUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Radarr API Key.
    /// </summary>
    public string RadarrApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Sonarr URL.
    /// </summary>
    public string SonarrUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Sonarr API Key.
    /// </summary>
    public string SonarrApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sync interval in hours.
    /// </summary>
    public int SyncIntervalHours { get; set; } = 6;

    /// <summary>
    /// Gets or sets the TMDB API Key.
    /// </summary>
    public string TmdbApiKey { get; set; } = string.Empty;
}
