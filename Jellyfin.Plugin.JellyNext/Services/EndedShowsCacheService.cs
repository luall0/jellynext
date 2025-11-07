using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Jellyfin.Plugin.JellyNext.Models.Common;
using Jellyfin.Plugin.JellyNext.Models.Trakt;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Services;

/// <summary>
/// Service for caching ended/canceled shows metadata that never expires.
/// Used to avoid re-querying Trakt for shows that have completed and won't have new seasons.
/// </summary>
public class EndedShowsCacheService
{
    private readonly ILogger<EndedShowsCacheService> _logger;
    private readonly ConcurrentDictionary<int, EndedShowMetadata> _endedShowsCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="EndedShowsCacheService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public EndedShowsCacheService(ILogger<EndedShowsCacheService> logger)
    {
        _logger = logger;
        _endedShowsCache = new ConcurrentDictionary<int, EndedShowMetadata>();
    }

    /// <summary>
    /// Checks if a show is marked as ended/canceled in the cache.
    /// </summary>
    /// <param name="tvdbId">The TVDB ID of the show.</param>
    /// <returns>True if the show is ended/canceled and cached, false otherwise.</returns>
    public bool IsShowEnded(int tvdbId)
    {
        return _endedShowsCache.ContainsKey(tvdbId);
    }

    /// <summary>
    /// Gets metadata for an ended/canceled show.
    /// </summary>
    /// <param name="tvdbId">The TVDB ID of the show.</param>
    /// <returns>Ended/canceled show metadata if found and not expired, null otherwise.</returns>
    public EndedShowMetadata? GetEndedShow(int tvdbId)
    {
        if (!_endedShowsCache.TryGetValue(tvdbId, out var metadata))
        {
            return null;
        }

        var config = Plugin.Instance?.Configuration;
        var expirationDays = config?.EndedShowsCacheExpirationDays ?? 7;

        if (metadata.IsExpired(expirationDays))
        {
            _logger.LogDebug(
                "Ended show cache expired for {Title} (TVDB: {TvdbId}), removing from cache",
                metadata.Title,
                tvdbId);
            _endedShowsCache.TryRemove(tvdbId, out _);
            return null;
        }

        return metadata;
    }

    /// <summary>
    /// Adds or updates an ended/canceled show in the cache.
    /// </summary>
    /// <param name="tvdbId">The TVDB ID of the show.</param>
    /// <param name="metadata">The show metadata.</param>
    public void AddEndedShow(int tvdbId, EndedShowMetadata metadata)
    {
        _endedShowsCache.AddOrUpdate(tvdbId, metadata, (_, _) => metadata);
        _logger.LogDebug("Added ended show to cache: {Title} (TVDB: {TvdbId})", metadata.Title, tvdbId);
    }

    /// <summary>
    /// Marks a show as ended/canceled in the cache.
    /// </summary>
    /// <param name="show">The Trakt show.</param>
    /// <param name="lastSeasonWatched">The last season watched by any user.</param>
    public void MarkShowAsEnded(TraktShow show, int lastSeasonWatched)
    {
        if (show.Ids.Tvdb == null || show.Ids.Tvdb == 0)
        {
            return;
        }

        var metadata = new EndedShowMetadata
        {
            Title = show.Title,
            Year = show.Year,
            TmdbId = show.Ids.Tmdb,
            ImdbId = show.Ids.Imdb,
            TvdbId = show.Ids.Tvdb,
            TraktId = show.Ids.Trakt,
            Status = show.Status ?? "ended",
            Genres = show.Genres,
            LastSeasonWatched = lastSeasonWatched,
            CachedAt = DateTime.UtcNow
        };

        AddEndedShow(show.Ids.Tvdb.Value, metadata);
    }

    /// <summary>
    /// Removes a show from the ended/canceled shows cache (e.g., if it gets renewed).
    /// </summary>
    /// <param name="tvdbId">The TVDB ID of the show.</param>
    public void RemoveEndedShow(int tvdbId)
    {
        if (_endedShowsCache.TryRemove(tvdbId, out var metadata))
        {
            _logger.LogInformation("Removed show from ended cache: {Title} (TVDB: {TvdbId})", metadata.Title, tvdbId);
        }
    }

    /// <summary>
    /// Gets all ended/canceled shows in the cache.
    /// </summary>
    /// <returns>Dictionary of TVDB ID to ended/canceled show metadata.</returns>
    public IReadOnlyDictionary<int, EndedShowMetadata> GetAllEndedShows()
    {
        return _endedShowsCache;
    }

    /// <summary>
    /// Clears the entire ended/canceled shows cache.
    /// </summary>
    public void ClearCache()
    {
        _endedShowsCache.Clear();
        _logger.LogInformation("Cleared all ended shows cache");
    }

    /// <summary>
    /// Gets the count of ended/canceled shows in the cache.
    /// </summary>
    /// <returns>Number of ended/canceled shows cached.</returns>
    public int GetCachedCount()
    {
        return _endedShowsCache.Count;
    }

    /// <summary>
    /// Removes expired shows from the cache based on configured expiration days.
    /// </summary>
    /// <returns>Number of expired shows removed.</returns>
    public int RemoveExpiredShows()
    {
        var config = Plugin.Instance?.Configuration;
        var expirationDays = config?.EndedShowsCacheExpirationDays ?? 7;

        var expiredKeys = new List<int>();
        foreach (var kvp in _endedShowsCache)
        {
            if (kvp.Value.IsExpired(expirationDays))
            {
                expiredKeys.Add(kvp.Key);
            }
        }

        var removedCount = 0;
        foreach (var key in expiredKeys)
        {
            if (_endedShowsCache.TryRemove(key, out var metadata))
            {
                _logger.LogDebug(
                    "Removed expired show from cache: {Title} (TVDB: {TvdbId}, Age: {Age} days)",
                    metadata.Title,
                    key,
                    (DateTime.UtcNow - metadata.CachedAt).TotalDays);
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            _logger.LogInformation("Removed {Count} expired shows from ended shows cache", removedCount);
        }

        return removedCount;
    }
}
