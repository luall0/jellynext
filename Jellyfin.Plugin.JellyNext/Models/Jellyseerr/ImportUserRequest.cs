using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Jellyseerr;

/// <summary>
/// Request to import user(s) from Jellyfin.
/// </summary>
public class ImportUserRequest
{
    /// <summary>
    /// Gets or sets the Jellyfin user IDs to import.
    /// </summary>
    [JsonPropertyName("jellyfinUserIds")]
    public string[] JellyfinUserIds { get; set; } = System.Array.Empty<string>();
}
