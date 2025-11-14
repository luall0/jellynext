using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Jellyseerr;

/// <summary>
/// Response from Jellyseerr /service/radarr/{radarrId} endpoint.
/// </summary>
public class RadarrServiceResponse
{
    /// <summary>
    /// Gets or sets the server details.
    /// </summary>
    [JsonPropertyName("server")]
    public RadarrServer? Server { get; set; }

    /// <summary>
    /// Gets or sets the available quality profiles.
    /// </summary>
    [JsonPropertyName("profiles")]
    public List<QualityProfile> Profiles { get; set; } = new List<QualityProfile>();

    /// <summary>
    /// Gets or sets the available root folders.
    /// </summary>
    [JsonPropertyName("rootFolders")]
    public List<RootFolder> RootFolders { get; set; } = new List<RootFolder>();

    /// <summary>
    /// Gets or sets the available tags.
    /// </summary>
    [JsonPropertyName("tags")]
    public List<Tag> Tags { get; set; } = new List<Tag>();
}
