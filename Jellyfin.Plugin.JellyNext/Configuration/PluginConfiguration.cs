using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.JellyNext.Models;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.JellyNext.Configuration;

/// <summary>
/// Plugin configuration for JellyNext.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the Radarr URL.
    /// </summary>
    public string RadarrUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Radarr API Key.
    /// </summary>
    public string RadarrApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Sonarr URL.
    /// </summary>
    public string SonarrUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Sonarr API Key.
    /// </summary>
    public string SonarrApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cache expiration interval in hours.
    /// </summary>
    public int CacheExpirationHours { get; set; } = 6;

    /// <summary>
    /// Gets or sets the TMDB API Key (optional - uses Jellyfin's key if not provided).
    /// </summary>
    public string TmdbApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to ignore collected items in recommendations.
    /// </summary>
    public bool IgnoreCollected { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to ignore watchlisted items in recommendations.
    /// </summary>
    public bool IgnoreWatchlisted { get; set; } = false;

    /// <summary>
    /// Gets or sets the array of per-user Trakt configurations.
    /// </summary>
    public TraktUser[] TraktUsers { get; set; } = Array.Empty<TraktUser>();

    /// <summary>
    /// Adds a new Trakt user configuration.
    /// </summary>
    /// <param name="userGuid">The Jellyfin user GUID.</param>
    public void AddUser(Guid userGuid)
    {
        var existingUser = TraktUsers.FirstOrDefault(u => u.LinkedMbUserId == userGuid);
        if (existingUser != null)
        {
            return;
        }

        var newUser = new TraktUser { LinkedMbUserId = userGuid };
        var userList = TraktUsers.ToList();
        userList.Add(newUser);
        TraktUsers = userList.ToArray();
    }

    /// <summary>
    /// Removes a Trakt user configuration.
    /// </summary>
    /// <param name="userGuid">The Jellyfin user GUID.</param>
    public void RemoveUser(Guid userGuid)
    {
        TraktUsers = TraktUsers.Where(u => u.LinkedMbUserId != userGuid).ToArray();
    }

    /// <summary>
    /// Gets all Trakt user configurations.
    /// </summary>
    /// <returns>A read-only list of Trakt users.</returns>
    public IReadOnlyList<TraktUser> GetAllTraktUsers()
    {
        return TraktUsers.ToList().AsReadOnly();
    }
}
