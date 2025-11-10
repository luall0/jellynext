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
    private readonly ShowsCacheService _showsCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendationsProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="traktApi">The Trakt API service.</param>
    /// <param name="showsCache">The shows cache service.</param>
    public RecommendationsProvider(
        ILogger<RecommendationsProvider> logger,
        TraktApi traktApi,
        ShowsCacheService showsCache)
    {
        _logger = logger;
        _traktApi = traktApi;
        _showsCache = showsCache;
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
            if (traktUser.SyncMovieRecommendations)
            {
                await FetchMovieRecommendationsAsync(traktUser, contentItems);
            }

            if (traktUser.SyncShowRecommendations)
            {
                await FetchShowRecommendationsAsync(traktUser, contentItems);
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

    private async Task FetchMovieRecommendationsAsync(TraktUser traktUser, List<ContentItem> contentItems)
    {
        var limit = Math.Clamp(traktUser.MovieRecommendationsLimit, 1, 100);
        var movies = await _traktApi.GetMovieRecommendations(
            traktUser,
            traktUser.IgnoreCollected,
            traktUser.IgnoreWatchlisted,
            limit: limit);

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

    private async Task FetchShowRecommendationsAsync(TraktUser traktUser, List<ContentItem> contentItems)
    {
        var limit = Math.Clamp(traktUser.ShowRecommendationsLimit, 1, 100);
        var shows = await _traktApi.GetShowRecommendations(
            traktUser,
            traktUser.IgnoreCollected,
            traktUser.IgnoreWatchlisted,
            limit: limit);

        foreach (var show in shows)
        {
            var contentItem = await ProcessShowRecommendationAsync(show, traktUser);
            contentItems.Add(contentItem);
        }
    }

    private async Task<ContentItem> ProcessShowRecommendationAsync(TraktShow show, TraktUser traktUser)
    {
        var isEnded = IsShowEnded(show);
        var airedSeasonCount = await GetAiredSeasonCountAsync(show, traktUser, isEnded);

        return new ContentItem
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
        };
    }

    private bool IsShowEnded(TraktShow show)
    {
        return !string.IsNullOrEmpty(show.Status) &&
               (show.Status.Equals("ended", StringComparison.OrdinalIgnoreCase) ||
                show.Status.Equals("canceled", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<int?> GetAiredSeasonCountAsync(TraktShow show, TraktUser traktUser, bool isEnded)
    {
        var cachedSeasonCount = TryGetCachedSeasonCount(show, isEnded);
        if (cachedSeasonCount.HasValue)
        {
            return cachedSeasonCount.Value;
        }

        return await FetchAndCacheSeasonCountAsync(show, traktUser, isEnded);
    }

    private int? TryGetCachedSeasonCount(TraktShow show, bool isEnded)
    {
        if (show.Ids.Tvdb == null || show.Ids.Tvdb <= 0)
        {
            return null;
        }

        var cachedShow = _showsCache.GetCachedShow(show.Ids.Tvdb.Value);
        if (cachedShow != null && cachedShow.Seasons.Count > 0)
        {
            var airedSeasonCount = cachedShow.Seasons.Values.Count(s => s.AiredEpisodes > 0);
            _logger.LogDebug(
                "Using cached season count for show: {Title} (TVDB: {TvdbId}, Status: {Status}, Seasons: {Seasons})",
                show.Title,
                show.Ids.Tvdb.Value,
                cachedShow.Status,
                airedSeasonCount);
            return airedSeasonCount;
        }

        return null;
    }

    private async Task<int?> FetchAndCacheSeasonCountAsync(TraktShow show, TraktUser traktUser, bool isEnded)
    {
        try
        {
            var seasons = await _traktApi.GetShowSeasons(traktUser, show.Ids.Trakt);
            var airedSeasonCount = seasons.Count(s => s.Number > 0 && s.AiredEpisodes > 0);

            _logger.LogDebug(
                "Show {Title} has {SeasonCount} aired seasons",
                show.Title,
                airedSeasonCount);

            // Note: ShowsCacheService caching is handled by NextSeasonsProvider's sync process
            // We don't cache here to avoid redundant caching logic

            return airedSeasonCount;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch season info for show {Title}, will use default", show.Title);
            return null;
        }
    }
}
