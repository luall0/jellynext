using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Radarr;

/// <summary>
/// Represents a Radarr quality profile.
/// </summary>
public class RadarrQualityProfile
{
    /// <summary>
    /// Gets or sets the quality profile ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the quality profile name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
