using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyNext.Models;

/// <summary>
/// Represents a Trakt OAuth refresh token request.
/// </summary>
public class TraktUserRefreshTokenRequest
{
    /// <summary>
    /// Gets or sets the refresh token.
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Trakt client ID.
    /// </summary>
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Trakt client secret.
    /// </summary>
    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the redirect URI (for device flow: "urn:ietf:wg:oauth:2.0:oob").
    /// </summary>
    [JsonPropertyName("redirect_uri")]
    public string RedirectUri { get; set; } = "urn:ietf:wg:oauth:2.0:oob";

    /// <summary>
    /// Gets or sets the grant type (should be "refresh_token").
    /// </summary>
    [JsonPropertyName("grant_type")]
    public string GrantType { get; set; } = "refresh_token";
}
