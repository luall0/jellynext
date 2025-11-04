using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models;

/// <summary>
/// Represents Sonarr system status.
/// </summary>
public class SonarrSystemStatus
{
    /// <summary>
    /// Gets or sets the Sonarr version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the API key is valid.
    /// </summary>
    [JsonPropertyName("isApiKeyValid")]
    public bool IsApiKeyValid { get; set; }
}
