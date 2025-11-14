using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Jellyseerr;

/// <summary>
/// Jellyseerr media request response.
/// </summary>
public class MediaRequestResponse
{
    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the request status.
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; set; }

    /// <summary>
    /// Gets or sets the media information.
    /// </summary>
    [JsonPropertyName("media")]
    public MediaInfo? Media { get; set; }
}
