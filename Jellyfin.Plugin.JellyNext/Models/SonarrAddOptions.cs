using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models;

/// <summary>
/// Represents add options when adding a series to Sonarr.
/// </summary>
public class SonarrAddOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to search for missing episodes immediately.
    /// </summary>
    [JsonPropertyName("searchForMissingEpisodes")]
    public bool SearchForMissingEpisodes { get; set; }
}
