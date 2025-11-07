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
            // Fetch watched shows from Trakt
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
                    _logger.LogInformation(
                        "Processing show: {Title} (Trakt ID: {TraktId}, TVDB ID: {TvdbId}, Status: {Status})",
                        watchedShow.Show.Title,
                        watchedShow.Show.Ids.Trakt,
                        watchedShow.Show.Ids.Tvdb,
                        watchedShow.Show.Status ?? "unknown");

                    // Skip shows without TVDB ID (we need it to match with local library)
                    if (watchedShow.Show.Ids.Tvdb == null || watchedShow.Show.Ids.Tvdb == 0)
                    {
                        continue;
                    }

                    var tvdbId = watchedShow.Show.Ids.Tvdb.Value;

                    // Find the highest watched season (excluding specials - season 0)
                    var watchedSeasons = watchedShow.Seasons
                        .Where(s => s.Number > 0 && s.Episodes.Any())
                        .Select(s => s.Number)
                        .OrderByDescending(s => s)
                        .ToList();

                    if (!watchedSeasons.Any())
                    {
                        continue;
                    }

                    var highestWatchedSeason = watchedSeasons.First();

                    // Check if show is marked as ended or canceled
                    var isEnded = !string.IsNullOrEmpty(watchedShow.Show.Status) &&
                                  (watchedShow.Show.Status.Equals("ended", StringComparison.OrdinalIgnoreCase) ||
                                   watchedShow.Show.Status.Equals("canceled", StringComparison.OrdinalIgnoreCase));

                    // If show is ended/canceled, check the cache
                    if (isEnded && _endedShowsCache.IsShowEnded(tvdbId))
                    {
                        var cachedMetadata = _endedShowsCache.GetEndedShow(tvdbId);
                        if (cachedMetadata != null)
                        {
                            // Update last watched season if this user has progressed further
                            if (highestWatchedSeason > cachedMetadata.LastSeasonWatched)
                            {
                                _endedShowsCache.MarkShowAsEnded(watchedShow.Show, highestWatchedSeason);
                            }

                            _logger.LogDebug(
                                "Skipping ended/canceled show from cache: {Title} (TVDB: {TvdbId}, Status: {Status})",
                                watchedShow.Show.Title,
                                tvdbId,
                                cachedMetadata.Status);
                            continue;
                        }
                    }

                    // Get all available seasons from Trakt to verify the next season exists
                    TraktSeason[]? availableSeasons = null;
                    try
                    {
                        availableSeasons = await _traktApi.GetShowSeasons(traktUser, watchedShow.Show.Ids.Trakt);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to get seasons for {Title} (Trakt ID: {TraktId})",
                            watchedShow.Show.Title,
                            watchedShow.Show.Ids.Trakt);
                        continue;
                    }

                    if (availableSeasons == null || availableSeasons.Length == 0)
                    {
                        continue;
                    }

                    // Create a set of available season numbers (excluding specials - season 0)
                    // Only include seasons that have at least one episode that has already aired
                    var availableSeasonNumbers = availableSeasons
                        .Where(s => s.Number > 0 && s.AiredEpisodes > 0)
                        .Select(s => s.Number)
                        .ToHashSet();

                    var nextSeasonNumber = highestWatchedSeason + 1;

                    // Check if this next season has been released (exists in Trakt)
                    if (!availableSeasonNumbers.Contains(nextSeasonNumber))
                    {
                        // If show is ended/canceled and no next season exists, cache it
                        if (isEnded)
                        {
                            _endedShowsCache.MarkShowAsEnded(watchedShow.Show, highestWatchedSeason);
                            _logger.LogInformation(
                                "Cached ended/canceled show with no more seasons: {Title} (TVDB: {TvdbId}, Status: {Status})",
                                watchedShow.Show.Title,
                                tvdbId,
                                watchedShow.Show.Status);
                        }

                        continue;
                    }

                    // Check if this next season exists locally
                    var existsLocally = _localLibraryService.DoesSeasonExist(tvdbId, nextSeasonNumber);

                    if (!existsLocally)
                    {
                        contentItems.Add(new ContentItem
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
                    else if (isEnded)
                    {
                        // If show is ended/canceled and next season exists locally, cache it
                        _endedShowsCache.MarkShowAsEnded(watchedShow.Show, nextSeasonNumber);
                        _logger.LogInformation(
                            "Cached ended/canceled show with season {Season} in library: {Title} (TVDB: {TvdbId}, Status: {Status})",
                            nextSeasonNumber,
                            watchedShow.Show.Title,
                            tvdbId,
                            watchedShow.Show.Status);
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
}
