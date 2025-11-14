using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Jellyseerr;

/// <summary>
/// Jellyseerr status response.
/// </summary>
public class StatusResponse
{
    /// <summary>
    /// Gets or sets the version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the commit tag.
    /// </summary>
    [JsonPropertyName("commitTag")]
    public string CommitTag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether update is available.
    /// </summary>
    [JsonPropertyName("updateAvailable")]
    public bool UpdateAvailable { get; set; }

    /// <summary>
    /// Gets or sets the commit branch.
    /// </summary>
    [JsonPropertyName("commitsBehind")]
    public int CommitsBehind { get; set; }
}
