using System;
using System.Linq;
using Jellyfin.Plugin.JellyNext.Models.Common;
using Jellyfin.Plugin.JellyNext.Models.Radarr;
using Jellyfin.Plugin.JellyNext.Models.Sonarr;
using Jellyfin.Plugin.JellyNext.Models.Trakt;

namespace Jellyfin.Plugin.JellyNext.Helpers;

/// <summary>
/// Helper class for retrieving per-user Trakt configurations.
/// </summary>
public static class UserHelper
{
    /// <summary>
    /// Gets the Trakt user configuration for a Jellyfin user.
    /// </summary>
    /// <param name="userId">The Jellyfin user ID as a string.</param>
    /// <param name="authorized">If true, only return if user has valid access token.</param>
    /// <returns>The TraktUser configuration or null if not found.</returns>
    public static TraktUser? GetTraktUser(string userId, bool authorized = false)
    {
        return GetTraktUser(Guid.Parse(userId), authorized);
    }

    /// <summary>
    /// Gets the Trakt user configuration for a Jellyfin user.
    /// </summary>
    /// <param name="userGuid">The Jellyfin user GUID.</param>
    /// <param name="authorized">If true, only return if user has valid access token.</param>
    /// <returns>The TraktUser configuration or null if not found.</returns>
    public static TraktUser? GetTraktUser(Guid userGuid, bool authorized = false)
    {
        var config = Plugin.Instance?.Configuration;

        if (config?.TraktUsers == null || config.TraktUsers.Length == 0)
        {
            return null;
        }

        return config.TraktUsers.FirstOrDefault(user =>
            !user.LinkedMbUserId.Equals(Guid.Empty) &&
            user.LinkedMbUserId.Equals(userGuid) &&
            (!authorized || !string.IsNullOrEmpty(user.AccessToken)));
    }
}
