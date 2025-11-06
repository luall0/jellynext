using System;

namespace Jellyfin.Plugin.JellyNext.Models.Trakt;

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
    /// Gets or sets a value indicating whether to sync movie recommendations for this user.
    /// </summary>
    public bool SyncMovieRecommendations { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to sync show recommendations for this user.
    /// </summary>
    public bool SyncShowRecommendations { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to sync next seasons for this user.
    /// </summary>
    public bool SyncNextSeasons { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to ignore collected items in recommendations for this user.
    /// </summary>
    public bool IgnoreCollected { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to ignore watchlisted items in recommendations for this user.
    /// </summary>
    public bool IgnoreWatchlisted { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to limit show recommendations to season 1 only (improves Jellyfin scan performance).
    /// </summary>
    public bool LimitShowsToSeasonOne { get; set; } = true;
}
