using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Radarr;

/// <summary>
/// Represents Radarr system status.
/// </summary>
public class RadarrSystemStatus
{
    /// <summary>
    /// Gets or sets the Radarr version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the API key is valid.
    /// </summary>
    [JsonPropertyName("isApiKeyValid")]
    public bool IsApiKeyValid { get; set; }
}
