using System;

namespace Jellyfin.Plugin.JellyNext.Models;

/// <summary>
/// Represents a Jellyfin user's Trakt account configuration and OAuth tokens.
/// </summary>
public class TraktUser
{
    /// <summary>
    /// Gets or sets the OAuth access token.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OAuth refresh token.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the linked Jellyfin user ID.
    /// </summary>
    public Guid LinkedMbUserId { get; set; }

    /// <summary>
    /// Gets or sets the access token expiration timestamp.
    /// </summary>
    public DateTime AccessTokenExpiration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable extra logging for this user.
    /// </summary>
    public bool ExtraLogging { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to sync recommendations for this user.
    /// </summary>
    public bool SyncRecommendations { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to sync new seasons for this user.
    /// </summary>
    public bool SyncNewSeasons { get; set; } = true;
}
