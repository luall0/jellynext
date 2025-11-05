using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models;

/// <summary>
/// Represents extended show information from Trakt.
/// </summary>
public class TraktShowSummary
{
    /// <summary>
    /// Gets or sets the show title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the show year.
    /// </summary>
    [JsonPropertyName("year")]
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the number of aired seasons.
    /// </summary>
    [JsonPropertyName("aired_episodes")]
    public int AiredEpisodes { get; set; }

    /// <summary>
    /// Gets or sets the show IDs.
    /// </summary>
    [JsonPropertyName("ids")]
    public TraktIds Ids { get; set; } = new TraktIds();
}
