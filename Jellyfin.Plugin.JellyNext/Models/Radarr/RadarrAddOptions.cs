using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Radarr;

/// <summary>
/// Represents add options for a Radarr movie.
/// </summary>
public class RadarrAddOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to search for the movie immediately.
    /// </summary>
    [JsonPropertyName("searchForMovie")]
    public bool SearchForMovie { get; set; }
}
