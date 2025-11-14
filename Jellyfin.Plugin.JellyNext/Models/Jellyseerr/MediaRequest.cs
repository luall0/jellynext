using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Jellyseerr;

/// <summary>
/// Jellyseerr media request.
/// </summary>
public class MediaRequest
{
    /// <summary>
    /// Gets or sets the media type (movie or tv).
    /// </summary>
    [JsonPropertyName("mediaType")]
    public string MediaType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the TMDB media ID.
    /// </summary>
    [JsonPropertyName("mediaId")]
    public int MediaId { get; set; }

    /// <summary>
    /// Gets or sets the seasons to request (for TV shows). Can be "all" or array of season numbers.
    /// Should not be included for movies.
    /// </summary>
    [JsonPropertyName("seasons")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Seasons { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a 4K request.
    /// </summary>
    [JsonPropertyName("is4k")]
    public bool Is4k { get; set; }

    /// <summary>
    /// Gets or sets the user ID making the request.
    /// </summary>
    [JsonPropertyName("userId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the Radarr/Sonarr server ID to use.
    /// </summary>
    [JsonPropertyName("serverId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ServerId { get; set; }

    /// <summary>
    /// Gets or sets the quality profile ID to use.
    /// </summary>
    [JsonPropertyName("profileId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ProfileId { get; set; }

    /// <summary>
    /// Gets or sets the root folder path.
    /// </summary>
    [JsonPropertyName("rootFolder")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RootFolder { get; set; }
}
