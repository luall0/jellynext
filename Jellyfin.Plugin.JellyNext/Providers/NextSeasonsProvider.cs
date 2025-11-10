using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Helpers;
using Jellyfin.Plugin.JellyNext.Models.Common;
using Jellyfin.Plugin.JellyNext.Models.Trakt;
using Jellyfin.Plugin.JellyNext.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Providers;

/// <summary>
/// Provider for next seasons of watched shows.
/// </summary>
public class NextSeasonsProvider : IContentProvider
{
    private readonly ILogger<NextSeasonsProvider> _logger;
    private readonly TraktApi _traktApi;
    private readonly LocalLibraryService _localLibraryService;
    private readonly ShowsCacheService _showsCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="NextSeasonsProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="traktApi">The Trakt API service.</param>
    /// <param name="localLibraryService">The local library service.</param>
    /// <param name="showsCache">The shows cache service.</param>
    public NextSeasonsProvider(
        ILogger<NextSeasonsProvider> logger,
        TraktApi traktApi,
        LocalLibraryService localLibraryService,
        ShowsCacheService showsCache)
    {
        _logger = logger;
        _traktApi = traktApi;
        _localLibraryService = localLibraryService;
        _showsCache = showsCache;
    }

    /// <inheritdoc />
    public string ProviderName => "nextseasons";

    /// <inheritdoc />
    public string LibraryName => "Next Seasons";

    /// <inheritdoc />
    public bool IsEnabledForUser(Guid userId)
    {
        var traktUser = UserHelper.GetTraktUser(userId);
        if (traktUser == null || string.IsNullOrWhiteSpace(traktUser.AccessToken))
        {
            return false;
        }

        return traktUser.SyncNextSeasons;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentItem>> FetchContentAsync(Guid userId)
    {
        var traktUser = UserHelper.GetTraktUser(userId);
        if (traktUser == null)
        {
            _logger.LogWarning("No Trakt user found for Jellyfin user {UserId}", userId);
            return Array.Empty<ContentItem>();
        }

        // Perform sync (full or incremental) - this populates the cache with watched progress
        await SyncWatchedShows(traktUser);

        var contentItems = new List<ContentItem>();

        try
        {
            // Get all shows with watched progress from cache for this user (no duplicate API calls!)
            var watchedShows = _showsCache.GetShowsWithWatchedProgress(userId);
            var watchedShowsList = watchedShows.ToList();
            _logger.LogInformation("Processing {Count} watched shows from cache for user {UserId}", watchedShowsList.Count, userId);

            if (watchedShowsList.Count == 0)
            {
                return Array.Empty<ContentItem>();
            }

            foreach (var (show, highestWatchedSeason) in watchedShowsList)
            {
                try
                {
                    var contentItem = await ProcessWatchedShowAsync(show, highestWatchedSeason, traktUser);
                    if (contentItem != null)
                    {
                        contentItems.Add(contentItem);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Error processing watched show {Title}",
                        show.Title);
                }
            }

            _logger.LogInformation(
                "Found {Count} next season recommendations for user {UserId}",
                contentItems.Count,
                userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch next seasons for user {UserId}", userId);
        }

        return contentItems.AsReadOnly();
    }

    /// <summary>
    /// Syncs watched shows (automatically performs full or incremental sync based on cache state).
    /// </summary>
    /// <param name="traktUser">The Trakt user configuration.</param>
    private async Task SyncWatchedShows(TraktUser traktUser)
    {
        // ShowsCacheService handles full vs incremental logic internally
        await _showsCache.PerformIncrementalSync(traktUser);
    }

    /// <summary>
    /// Processes a watched show to determine if next season should be recommended.
    /// </summary>
    private async Task<ContentItem?> ProcessWatchedShowAsync(ShowCacheEntry cachedShow, int highestWatchedSeason, TraktUser traktUser)
    {
        if (!cachedShow.TvdbId.HasValue || cachedShow.TvdbId.Value == 0)
        {
            return null;
        }

        var tvdbId = cachedShow.TvdbId.Value;
        var nextSeasonNumber = highestWatchedSeason + 1;

        _logger.LogDebug(
            "Checking next season for {Title} (TVDB: {TvdbId}): highest watched S{Watched}, checking S{Next}",
            cachedShow.Title,
            tvdbId,
            highestWatchedSeason,
            nextSeasonNumber);

        // Check cache for next season
        var cachedSeason = _showsCache.GetCachedSeason(tvdbId, nextSeasonNumber);

        // If not in cache and show is not ended, fetch fresh data
        if (cachedSeason == null && !cachedShow.IsEnded)
        {
            _logger.LogDebug(
                "Next season S{Season} not in cache for ongoing show {Title}, fetching from Trakt",
                nextSeasonNumber,
                cachedShow.Title);

            // Fetch latest seasons from Trakt
            var traktSeasons = await _traktApi.GetShowSeasons(traktUser, cachedShow.TraktId);
            var nextTraktSeason = traktSeasons.FirstOrDefault(s => s.Number == nextSeasonNumber);

            if (nextTraktSeason != null && nextTraktSeason.AiredEpisodes > 0)
            {
                cachedSeason = new SeasonMetadata
                {
                    SeasonNumber = nextTraktSeason.Number,
                    EpisodeCount = nextTraktSeason.EpisodeCount,
                    AiredEpisodes = nextTraktSeason.AiredEpisodes,
                    FirstAired = nextTraktSeason.FirstAired,
                    CachedAt = DateTime.UtcNow
                };
            }
            else
            {
                _logger.LogDebug(
                    "Next season S{Season} does not exist or has not aired for {Title}",
                    nextSeasonNumber,
                    cachedShow.Title);
                return null;
            }
        }

        // If season not found (even after fetch), return null
        if (cachedSeason == null)
        {
            return null;
        }

        // Check if season has aired
        if (cachedSeason.AiredEpisodes == 0)
        {
            _logger.LogDebug(
                "Next season S{Season} has not aired yet for {Title}",
                nextSeasonNumber,
                cachedShow.Title);
            return null;
        }

        // Check if season exists in local library
        var existsLocally = _localLibraryService.DoesSeasonExist(tvdbId, nextSeasonNumber);
        if (existsLocally)
        {
            _logger.LogDebug(
                "Next season S{Season} already exists locally for {Title}",
                nextSeasonNumber,
                cachedShow.Title);
            return null;
        }

        // Recommend the next season
        _logger.LogInformation(
            "Recommending next season S{Season} for {Title} (TVDB: {TvdbId})",
            nextSeasonNumber,
            cachedShow.Title,
            tvdbId);

        return new ContentItem
        {
            Type = ContentType.Show,
            Title = cachedShow.Title,
            Year = cachedShow.Year,
            TmdbId = cachedShow.TmdbId,
            ImdbId = cachedShow.ImdbId,
            TvdbId = cachedShow.TvdbId,
            TraktId = cachedShow.TraktId,
            ProviderName = ProviderName,
            SeasonNumber = nextSeasonNumber,
            Genres = cachedShow.Genres
        };
    }
}
