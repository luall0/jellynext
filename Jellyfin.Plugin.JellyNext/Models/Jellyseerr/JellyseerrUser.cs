using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models.Jellyseerr;

/// <summary>
/// Jellyseerr user.
/// </summary>
public class JellyseerrUser
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin username.
    /// </summary>
    [JsonPropertyName("jellyfinUsername")]
    public string? JellyfinUsername { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin user ID.
    /// </summary>
    [JsonPropertyName("jellyfinUserId")]
    public string? JellyfinUserId { get; set; }

    /// <summary>
    /// Gets or sets the user type.
    /// </summary>
    [JsonPropertyName("userType")]
    public int UserType { get; set; }

    /// <summary>
    /// Gets or sets the permissions bitmask.
    /// </summary>
    [JsonPropertyName("permissions")]
    public int Permissions { get; set; }

    /// <summary>
    /// Gets or sets the request count.
    /// </summary>
    [JsonPropertyName("requestCount")]
    public int RequestCount { get; set; }
}
