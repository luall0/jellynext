using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Helpers;
using Jellyfin.Plugin.JellyNext.Models.Common;
using Jellyfin.Plugin.JellyNext.Models.Radarr;
using Jellyfin.Plugin.JellyNext.Models.Sonarr;
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
    private readonly EndedShowsCacheService _endedShowsCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="NextSeasonsProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="traktApi">The Trakt API service.</param>
    /// <param name="localLibraryService">The local library service.</param>
    /// <param name="endedShowsCache">The ended shows cache service.</param>
    public NextSeasonsProvider(
        ILogger<NextSeasonsProvider> logger,
        TraktApi traktApi,
        LocalLibraryService localLibraryService,
        EndedShowsCacheService endedShowsCache)
    {
        _logger = logger;
        _traktApi = traktApi;
        _localLibraryService = localLibraryService;
        _endedShowsCache = endedShowsCache;
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

        var contentItems = new List<ContentItem>();

        try
        {
            var watchedShows = await _traktApi.GetWatchedShows(traktUser);
            _logger.LogInformation("Found {Count} watched shows for user {UserId}", watchedShows.Length, userId);

            if (watchedShows.Length == 0)
            {
                return Array.Empty<ContentItem>();
            }

            foreach (var watchedShow in watchedShows)
            {
                try
                {
                    var contentItem = await ProcessWatchedShowAsync(watchedShow, userId);
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
                        watchedShow.Show.Title);
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

    private async Task<ContentItem?> ProcessWatchedShowAsync(TraktWatchedShow watchedShow, Guid userId)
    {
        _logger.LogInformation(
            "Processing show: {Title} (Trakt ID: {TraktId}, TVDB ID: {TvdbId}, Status: {Status})",
            watchedShow.Show.Title,
            watchedShow.Show.Ids.Trakt,
            watchedShow.Show.Ids.Tvdb,
            watchedShow.Show.Status ?? "unknown");

        if (watchedShow.Show.Ids.Tvdb == null || watchedShow.Show.Ids.Tvdb == 0)
        {
            return null;
        }

        var tvdbId = watchedShow.Show.Ids.Tvdb.Value;
        var highestWatchedSeason = GetHighestWatchedSeason(watchedShow);
        if (!highestWatchedSeason.HasValue)
        {
            return null;
        }

        var nextSeasonNumber = highestWatchedSeason.Value + 1;
        var isEnded = IsShowEnded(watchedShow.Show);

        if (await ShouldSkipCachedShowAsync(watchedShow, tvdbId, highestWatchedSeason.Value, nextSeasonNumber, isEnded))
        {
            return null;
        }

        var traktUser = UserHelper.GetTraktUser(userId);
        var availableSeasons = await GetAvailableSeasonsAsync(watchedShow, traktUser!);
        if (availableSeasons == null)
        {
            return null;
        }

        var availableSeasonNumbers = GetAiredSeasonNumbers(availableSeasons);

        if (!availableSeasonNumbers.Contains(nextSeasonNumber))
        {
            CacheEndedShowIfApplicable(watchedShow, isEnded, highestWatchedSeason.Value, tvdbId);
            return null;
        }

        return await CreateContentItemIfNeededAsync(watchedShow, nextSeasonNumber, tvdbId, isEnded);
    }

    private int? GetHighestWatchedSeason(TraktWatchedShow watchedShow)
    {
        var watchedSeasons = watchedShow.Seasons
            .Where(s => s.Number > 0 && s.Episodes.Any())
            .Select(s => s.Number)
            .OrderByDescending(s => s)
            .ToList();

        return watchedSeasons.Any() ? watchedSeasons.First() : null;
    }

    private bool IsShowEnded(TraktShow show)
    {
        return !string.IsNullOrEmpty(show.Status) &&
               (show.Status.Equals("ended", StringComparison.OrdinalIgnoreCase) ||
                show.Status.Equals("canceled", StringComparison.OrdinalIgnoreCase));
    }

    private Task<bool> ShouldSkipCachedShowAsync(
        TraktWatchedShow watchedShow,
        int tvdbId,
        int highestWatchedSeason,
        int nextSeasonNumber,
        bool isEnded)
    {
        if (!isEnded || !_endedShowsCache.IsShowEnded(tvdbId))
        {
            return Task.FromResult(false);
        }

        var cachedMetadata = _endedShowsCache.GetEndedShow(tvdbId);
        if (cachedMetadata == null)
        {
            return Task.FromResult(false);
        }

        if (highestWatchedSeason <= cachedMetadata.LastSeasonWatched)
        {
            var existsLocally = _localLibraryService.DoesSeasonExist(tvdbId, nextSeasonNumber);

            if (!existsLocally)
            {
                _logger.LogDebug(
                    "Skipping ended/canceled show from cache: {Title} (TVDB: {TvdbId}, Status: {Status}, Last watched: S{Season})",
                    watchedShow.Show.Title,
                    tvdbId,
                    cachedMetadata.Status,
                    highestWatchedSeason);
                return Task.FromResult(true);
            }

            _logger.LogInformation(
                "Next season S{Season} found locally for cached show {Title} (TVDB: {TvdbId}), checking for newer seasons",
                nextSeasonNumber,
                watchedShow.Show.Title,
                tvdbId);
        }
        else
        {
            _logger.LogInformation(
                "User progressed beyond cached season for {Title} (TVDB: {TvdbId}): S{Old} -> S{New}, re-checking seasons",
                watchedShow.Show.Title,
                tvdbId,
                cachedMetadata.LastSeasonWatched,
                highestWatchedSeason);
        }

        return Task.FromResult(false);
    }

    private async Task<TraktSeason[]?> GetAvailableSeasonsAsync(TraktWatchedShow watchedShow, TraktUser traktUser)
    {
        try
        {
            return await _traktApi.GetShowSeasons(traktUser, watchedShow.Show.Ids.Trakt);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to get seasons for {Title} (Trakt ID: {TraktId})",
                watchedShow.Show.Title,
                watchedShow.Show.Ids.Trakt);
            return null;
        }
    }

    private HashSet<int> GetAiredSeasonNumbers(TraktSeason[] seasons)
    {
        return seasons
            .Where(s => s.Number > 0 && s.AiredEpisodes > 0)
            .Select(s => s.Number)
            .ToHashSet();
    }

    private void CacheEndedShowIfApplicable(TraktWatchedShow watchedShow, bool isEnded, int lastSeasonWatched, int tvdbId)
    {
        if (isEnded)
        {
            _endedShowsCache.MarkShowAsEnded(watchedShow.Show, lastSeasonWatched);
            _logger.LogInformation(
                "Cached ended/canceled show with no more seasons: {Title} (TVDB: {TvdbId}, Status: {Status})",
                watchedShow.Show.Title,
                tvdbId,
                watchedShow.Show.Status);
        }
    }

    private Task<ContentItem?> CreateContentItemIfNeededAsync(
        TraktWatchedShow watchedShow,
        int nextSeasonNumber,
        int tvdbId,
        bool isEnded)
    {
        var seasonExistsLocally = _localLibraryService.DoesSeasonExist(tvdbId, nextSeasonNumber);

        if (!seasonExistsLocally)
        {
            return Task.FromResult<ContentItem?>(new ContentItem
            {
                Type = ContentType.Show,
                Title = watchedShow.Show.Title,
                Year = watchedShow.Show.Year,
                TmdbId = watchedShow.Show.Ids.Tmdb,
                ImdbId = watchedShow.Show.Ids.Imdb,
                TvdbId = watchedShow.Show.Ids.Tvdb,
                TraktId = watchedShow.Show.Ids.Trakt,
                ProviderName = ProviderName,
                SeasonNumber = nextSeasonNumber,
                Genres = watchedShow.Show.Genres
            });
        }

        if (isEnded)
        {
            _endedShowsCache.MarkShowAsEnded(watchedShow.Show, nextSeasonNumber);
            _logger.LogInformation(
                "Cached ended/canceled show with season {Season} in library: {Title} (TVDB: {TvdbId}, Status: {Status})",
                nextSeasonNumber,
                watchedShow.Show.Title,
                tvdbId,
                watchedShow.Show.Status);
        }

        return Task.FromResult<ContentItem?>(null);
    }
}
