using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Sonarr;

/// <summary>
/// Represents a season in Sonarr.
/// </summary>
public class SonarrSeason
{
    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    [JsonPropertyName("seasonNumber")]
    public int SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the season is monitored.
    /// </summary>
    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }
}
