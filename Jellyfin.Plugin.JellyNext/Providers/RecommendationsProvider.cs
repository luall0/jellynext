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
/// Provider for Trakt personalized recommendations.
/// </summary>
public class RecommendationsProvider : IContentProvider
{
    private readonly ILogger<RecommendationsProvider> _logger;
    private readonly TraktApi _traktApi;
    private readonly EndedShowsCacheService _endedShowsCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendationsProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="traktApi">The Trakt API service.</param>
    /// <param name="endedShowsCache">The ended shows cache service.</param>
    public RecommendationsProvider(
        ILogger<RecommendationsProvider> logger,
        TraktApi traktApi,
        EndedShowsCacheService endedShowsCache)
    {
        _logger = logger;
        _traktApi = traktApi;
        _endedShowsCache = endedShowsCache;
    }

    /// <inheritdoc />
    public string ProviderName => "recommendations";

    /// <inheritdoc />
    public string LibraryName => "Trakt Recommendations";

    /// <inheritdoc />
    public bool IsEnabledForUser(Guid userId)
    {
        var traktUser = UserHelper.GetTraktUser(userId);
        if (traktUser == null || string.IsNullOrWhiteSpace(traktUser.AccessToken))
        {
            return false;
        }

        return traktUser.SyncMovieRecommendations || traktUser.SyncShowRecommendations;
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
            // Fetch movie recommendations if enabled for this user
            if (traktUser.SyncMovieRecommendations)
            {
                var movies = await _traktApi.GetMovieRecommendations(
                    traktUser,
                    traktUser.IgnoreCollected,
                    traktUser.IgnoreWatchlisted,
                    limit: 50);

                foreach (var movie in movies)
                {
                    contentItems.Add(new ContentItem
                    {
                        Type = ContentType.Movie,
                        Title = movie.Title,
                        Year = movie.Year,
                        TmdbId = movie.Ids.Tmdb,
                        ImdbId = movie.Ids.Imdb,
                        TraktId = movie.Ids.Trakt,
                        ProviderName = ProviderName,
                        Genres = movie.Genres
                    });
                }
            }

            // Fetch show recommendations if enabled for this user
            TraktShow[] shows = Array.Empty<TraktShow>();
            if (traktUser.SyncShowRecommendations)
            {
                shows = await _traktApi.GetShowRecommendations(
                    traktUser,
                    traktUser.IgnoreCollected,
                    traktUser.IgnoreWatchlisted,
                    limit: 50);

                foreach (var show in shows)
                {
                    // Check if show is ended/canceled and in cache
                    var isEnded = !string.IsNullOrEmpty(show.Status) &&
                                  (show.Status.Equals("ended", StringComparison.OrdinalIgnoreCase) ||
                                   show.Status.Equals("canceled", StringComparison.OrdinalIgnoreCase));

                    int? airedSeasonCount = null;

                    // If ended/canceled and in cache, use cached season count (skip API call)
                    if (isEnded && show.Ids.Tvdb != null && show.Ids.Tvdb > 0)
                    {
                        var cachedMetadata = _endedShowsCache.GetEndedShow(show.Ids.Tvdb.Value);
                        if (cachedMetadata != null)
                        {
                            airedSeasonCount = cachedMetadata.LastSeasonWatched;
                            _logger.LogDebug(
                                "Using cached season count for ended/canceled show: {Title} (TVDB: {TvdbId}, Seasons: {Seasons})",
                                show.Title,
                                show.Ids.Tvdb.Value,
                                airedSeasonCount);
                        }
                    }

                    // Only fetch season information if not in cache
                    if (!airedSeasonCount.HasValue)
                    {
                        try
                        {
                            var seasons = await _traktApi.GetShowSeasons(traktUser, show.Ids.Trakt);
                            // Count seasons that have aired episodes (excluding specials which are season 0)
                            airedSeasonCount = seasons.Count(s => s.Number > 0 && s.AiredEpisodes > 0);
                            _logger.LogDebug(
                                "Show {Title} has {SeasonCount} aired seasons",
                                show.Title,
                                airedSeasonCount);

                            // If show is ended/canceled and we fetched season info, cache it for future
                            if (isEnded && show.Ids.Tvdb != null && show.Ids.Tvdb > 0 && airedSeasonCount.HasValue)
                            {
                                _endedShowsCache.MarkShowAsEnded(show, airedSeasonCount.Value);
                                _logger.LogDebug(
                                    "Cached ended/canceled show from recommendations: {Title} (TVDB: {TvdbId}, Status: {Status}, Seasons: {Seasons})",
                                    show.Title,
                                    show.Ids.Tvdb.Value,
                                    show.Status,
                                    airedSeasonCount.Value);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to fetch season info for show {Title}, will use default", show.Title);
                        }
                    }

                    contentItems.Add(new ContentItem
                    {
                        Type = ContentType.Show,
                        Title = show.Title,
                        Year = show.Year,
                        TmdbId = show.Ids.Tmdb,
                        ImdbId = show.Ids.Imdb,
                        TvdbId = show.Ids.Tvdb,
                        TraktId = show.Ids.Trakt,
                        ProviderName = ProviderName,
                        AiredSeasonCount = airedSeasonCount,
                        Genres = show.Genres
                    });
                }
            }

            int movieCount = contentItems.Count(c => c.Type == ContentType.Movie);
            int showCount = contentItems.Count(c => c.Type == ContentType.Show);
            _logger.LogInformation(
                "Fetched {MovieCount} movie and {ShowCount} show recommendations for user {UserId}",
                movieCount,
                showCount,
                userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch recommendations for user {UserId}", userId);
        }

        return contentItems.AsReadOnly();
    }
}
