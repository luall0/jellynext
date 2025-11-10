using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Models.Common;
using Jellyfin.Plugin.JellyNext.Models.Trakt;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Services;

/// <summary>
/// Service for caching TV shows with season-level metadata.
/// Supports both full sync (first run) and incremental sync (subsequent runs).
/// </summary>
public class ShowsCacheService
{
    private readonly ILogger<ShowsCacheService> _logger;
    private readonly TraktApi _traktApi;

    // Global cache: tvdbId -> ShowCacheEntry (shared metadata/seasons)
    private readonly ConcurrentDictionary<int, ShowCacheEntry> _showsCache;

    // Per-user watch progress: userId -> (tvdbId -> highest watched season)
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<int, int>> _userWatchProgress;

    // Per-user last sync timestamp: userId -> timestamp (in-memory only, not persisted)
    private readonly ConcurrentDictionary<Guid, DateTime> _userLastSyncTimestamp;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowsCacheService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="traktApi">The Trakt API service.</param>
    public ShowsCacheService(ILogger<ShowsCacheService> logger, TraktApi traktApi)
    {
        _logger = logger;
        _traktApi = traktApi;
        _showsCache = new ConcurrentDictionary<int, ShowCacheEntry>();
        _userWatchProgress = new ConcurrentDictionary<Guid, ConcurrentDictionary<int, int>>();
        _userLastSyncTimestamp = new ConcurrentDictionary<Guid, DateTime>();
    }

    /// <summary>
    /// Gets or creates the watch progress dictionary for a specific user.
    /// </summary>
    private ConcurrentDictionary<int, int> GetUserWatchProgress(Guid userId)
    {
        return _userWatchProgress.GetOrAdd(userId, _ => new ConcurrentDictionary<int, int>());
    }

    /// <summary>
    /// Performs initial full sync for a user, caching all watched shows and their seasons.
    /// </summary>
    /// <param name="traktUser">The Trakt user configuration.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task PerformFullSync(TraktUser traktUser)
    {
        _logger.LogInformation("Performing full sync for user {UserId}", traktUser.LinkedMbUserId);

        var watchedShows = await _traktApi.GetWatchedShows(traktUser);
        _logger.LogInformation("Found {Count} watched shows for user {UserId}", watchedShows.Length, traktUser.LinkedMbUserId);

        foreach (var watchedShow in watchedShows)
        {
            if (watchedShow.Show.Ids.Tvdb == null || watchedShow.Show.Ids.Tvdb == 0)
            {
                continue;
            }

            var tvdbId = watchedShow.Show.Ids.Tvdb.Value;

            try
            {
                // Cache show metadata/seasons (global)
                await CacheShowWithSeasons(watchedShow.Show, traktUser);

                // Update user watch progress (per-user)
                var highestWatchedSeason = GetHighestWatchedSeason(watchedShow);
                if (highestWatchedSeason.HasValue)
                {
                    UpdateUserWatchProgress(traktUser.LinkedMbUserId, tvdbId, highestWatchedSeason.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to cache show {Title} (TVDB: {TvdbId})",
                    watchedShow.Show.Title,
                    tvdbId);
            }
        }

        // Set last sync timestamp to now - 1 minute for next incremental sync (in-memory only)
        _userLastSyncTimestamp[traktUser.LinkedMbUserId] = DateTime.UtcNow.AddMinutes(-1);

        var userProgress = GetUserWatchProgress(traktUser.LinkedMbUserId);
        _logger.LogInformation("Full sync completed for user {UserId}, cached {Count} shows with watch progress", traktUser.LinkedMbUserId, userProgress.Count);
    }

    /// <summary>
    /// Performs incremental sync using watch history since last sync.
    /// </summary>
    /// <param name="traktUser">The Trakt user configuration.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task PerformIncrementalSync(TraktUser traktUser)
    {
        if (!_userLastSyncTimestamp.TryGetValue(traktUser.LinkedMbUserId, out var lastSyncTimestamp))
        {
            _logger.LogInformation("No last sync timestamp, performing full sync instead");
            await PerformFullSync(traktUser);
            return;
        }

        var startAt = lastSyncTimestamp;
        var endAt = DateTime.UtcNow.AddMinutes(-1);

        _logger.LogInformation(
            "Performing incremental sync for user {UserId} from {StartAt} to {EndAt}",
            traktUser.LinkedMbUserId,
            startAt,
            endAt);

        var historyItems = await _traktApi.GetShowWatchHistory(traktUser, startAt, endAt);
        _logger.LogInformation("Found {Count} history items for user {UserId}", historyItems.Length, traktUser.LinkedMbUserId);

        // Group by show to get highest watched season from history
        var showsToUpdate = historyItems
            .Where(h => h.Show.Ids.Tvdb.HasValue && h.Show.Ids.Tvdb.Value > 0 && h.Episode != null)
            .GroupBy(h => h.Show.Ids.Tvdb!.Value)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Show = g.First().Show,
                    HighestSeason = g.Max(item => item.Episode?.Season ?? 0)
                });

        foreach (var (tvdbId, data) in showsToUpdate)
        {
            try
            {
                // Cache show metadata/seasons (global)
                await CacheShowWithSeasons(data.Show, traktUser);

                // Update user watch progress (per-user)
                if (data.HighestSeason > 0)
                {
                    UpdateUserWatchProgress(traktUser.LinkedMbUserId, tvdbId, data.HighestSeason);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to update cache for show {Title} (TVDB: {TvdbId})",
                    data.Show.Title,
                    tvdbId);
            }
        }

        // Update last sync timestamp (in-memory only)
        _userLastSyncTimestamp[traktUser.LinkedMbUserId] = endAt;

        _logger.LogInformation("Incremental sync completed for user {UserId}, updated {Count} shows", traktUser.LinkedMbUserId, showsToUpdate.Count);
    }

    /// <summary>
    /// Caches a show with its seasons based on show status.
    /// This caches global metadata/seasons only, not user-specific watch progress.
    /// </summary>
    /// <param name="show">The Trakt show.</param>
    /// <param name="traktUser">The Trakt user configuration.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CacheShowWithSeasons(TraktShow show, TraktUser traktUser)
    {
        if (show.Ids.Tvdb == null || show.Ids.Tvdb == 0)
        {
            return;
        }

        var tvdbId = show.Ids.Tvdb.Value;
        var isEnded = IsShowEnded(show);

        // Get all seasons from Trakt
        var traktSeasons = await _traktApi.GetShowSeasons(traktUser, show.Ids.Trakt);
        if (traktSeasons.Length == 0)
        {
            _logger.LogDebug("No seasons found for {Title} (TVDB: {TvdbId})", show.Title, tvdbId);
            return;
        }

        // Create or update cache entry (global)
        var cacheEntry = _showsCache.GetOrAdd(tvdbId, _ => new ShowCacheEntry
        {
            Title = show.Title,
            Year = show.Year,
            TmdbId = show.Ids.Tmdb,
            ImdbId = show.Ids.Imdb,
            TvdbId = show.Ids.Tvdb,
            TraktId = show.Ids.Trakt,
            Status = show.Status ?? "unknown",
            Genres = show.Genres ?? Array.Empty<string>(),
            CachedAt = DateTime.UtcNow
        });

        // Update show metadata
        cacheEntry.Status = show.Status ?? "unknown";
        cacheEntry.Genres = show.Genres ?? Array.Empty<string>();

        // Cache seasons based on show status
        var cachedSeasons = 0;
        foreach (var season in traktSeasons.Where(s => s.Number > 0))
        {
            if (isEnded)
            {
                // For ended/canceled shows: cache all seasons
                CacheSeason(cacheEntry, season);
                cachedSeasons++;
            }
            else
            {
                // For ongoing shows: only cache complete seasons
                if (season.EpisodeCount > 0 && season.EpisodeCount == season.AiredEpisodes)
                {
                    CacheSeason(cacheEntry, season);
                    cachedSeasons++;
                }
                else if (cacheEntry.Seasons.ContainsKey(season.Number))
                {
                    // Update incomplete season if already in cache (e.g., newly aired episodes)
                    CacheSeason(cacheEntry, season);
                }
            }
        }

        _logger.LogDebug(
            "Cached {Title} (TVDB: {TvdbId}, Status: {Status}): {CachedSeasons}/{TotalSeasons} seasons",
            show.Title,
            tvdbId,
            show.Status ?? "unknown",
            cachedSeasons,
            traktSeasons.Count(s => s.Number > 0));
    }

    /// <summary>
    /// Caches a single season's metadata.
    /// </summary>
    /// <param name="cacheEntry">The show cache entry.</param>
    /// <param name="traktSeason">The Trakt season.</param>
    private void CacheSeason(ShowCacheEntry cacheEntry, TraktSeason traktSeason)
    {
        cacheEntry.Seasons[traktSeason.Number] = new SeasonMetadata
        {
            SeasonNumber = traktSeason.Number,
            EpisodeCount = traktSeason.EpisodeCount,
            AiredEpisodes = traktSeason.AiredEpisodes,
            FirstAired = traktSeason.FirstAired,
            CachedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates or sets the user's watch progress for a show.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="tvdbId">The TVDB ID.</param>
    /// <param name="highestWatchedSeason">The highest season watched.</param>
    public void UpdateUserWatchProgress(Guid userId, int tvdbId, int highestWatchedSeason)
    {
        var userProgress = GetUserWatchProgress(userId);
        userProgress.AddOrUpdate(tvdbId, highestWatchedSeason, (_, existing) => Math.Max(existing, highestWatchedSeason));
    }

    /// <summary>
    /// Gets the user's highest watched season for a show.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="tvdbId">The TVDB ID.</param>
    /// <returns>The highest watched season if found, null otherwise.</returns>
    public int? GetUserHighestWatchedSeason(Guid userId, int tvdbId)
    {
        var userProgress = GetUserWatchProgress(userId);
        return userProgress.TryGetValue(tvdbId, out var season) ? season : null;
    }

    /// <summary>
    /// Gets a cached show by TVDB ID.
    /// </summary>
    /// <param name="tvdbId">The TVDB ID.</param>
    /// <returns>The cached show entry if found, null otherwise.</returns>
    public ShowCacheEntry? GetCachedShow(int tvdbId)
    {
        return _showsCache.TryGetValue(tvdbId, out var entry) ? entry : null;
    }

    /// <summary>
    /// Gets cached metadata for a specific season.
    /// </summary>
    /// <param name="tvdbId">The TVDB ID.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <returns>The cached season metadata if found, null otherwise.</returns>
    public SeasonMetadata? GetCachedSeason(int tvdbId, int seasonNumber)
    {
        var show = GetCachedShow(tvdbId);
        return show?.Seasons.TryGetValue(seasonNumber, out var season) == true ? season : null;
    }

    /// <summary>
    /// Checks if a season exists in the cache and has aired.
    /// </summary>
    /// <param name="tvdbId">The TVDB ID.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <returns>True if the season exists and has aired, false otherwise.</returns>
    public bool IsSeasonAvailable(int tvdbId, int seasonNumber)
    {
        var season = GetCachedSeason(tvdbId, seasonNumber);
        return season != null && season.AiredEpisodes > 0;
    }

    /// <summary>
    /// Removes a show from the cache.
    /// </summary>
    /// <param name="tvdbId">The TVDB ID.</param>
    public void RemoveShow(int tvdbId)
    {
        if (_showsCache.TryRemove(tvdbId, out var entry))
        {
            _logger.LogInformation("Removed show from cache: {Title} (TVDB: {TvdbId})", entry.Title, tvdbId);
        }
    }

    /// <summary>
    /// Clears the entire cache.
    /// </summary>
    public void ClearCache()
    {
        _showsCache.Clear();
        _logger.LogInformation("Cleared all shows cache");
    }

    /// <summary>
    /// Gets the count of cached shows.
    /// </summary>
    /// <returns>Number of shows in cache.</returns>
    public int GetCachedShowCount()
    {
        return _showsCache.Count;
    }

    /// <summary>
    /// Gets all cached shows.
    /// </summary>
    /// <returns>Dictionary of TVDB ID to show cache entry.</returns>
    public IReadOnlyDictionary<int, ShowCacheEntry> GetAllCachedShows()
    {
        return _showsCache;
    }

    /// <summary>
    /// Gets all cached shows that have watched progress for a specific user.
    /// Returns tuples of (show, highestWatchedSeason).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>Collection of shows with their watched progress for the user.</returns>
    public IEnumerable<(ShowCacheEntry Show, int HighestWatchedSeason)> GetShowsWithWatchedProgress(Guid userId)
    {
        var userProgress = GetUserWatchProgress(userId);
        foreach (var (tvdbId, highestSeason) in userProgress)
        {
            var show = GetCachedShow(tvdbId);
            if (show != null)
            {
                yield return (show, highestSeason);
            }
        }
    }

    /// <summary>
    /// Determines if a show is ended or canceled.
    /// </summary>
    /// <param name="show">The Trakt show.</param>
    /// <returns>True if ended or canceled, false otherwise.</returns>
    private bool IsShowEnded(TraktShow show)
    {
        return !string.IsNullOrEmpty(show.Status) &&
               (show.Status.Equals("ended", StringComparison.OrdinalIgnoreCase) ||
                show.Status.Equals("canceled", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the highest watched season from a watched show.
    /// </summary>
    /// <param name="watchedShow">The watched show data.</param>
    /// <returns>The highest season number with watched episodes, or null if none.</returns>
    private int? GetHighestWatchedSeason(TraktWatchedShow watchedShow)
    {
        var watchedSeasons = watchedShow.Seasons
            .Where(s => s.Number > 0 && s.Episodes.Any())
            .Select(s => s.Number)
            .OrderByDescending(s => s)
            .ToList();

        return watchedSeasons.Any() ? watchedSeasons.First() : null;
    }
}
