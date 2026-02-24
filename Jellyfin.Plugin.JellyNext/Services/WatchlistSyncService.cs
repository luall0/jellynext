using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Helpers;
using Jellyfin.Plugin.JellyNext.Models.Common;
using Jellyfin.Plugin.JellyNext.Services.DownloadProviders;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Services;

/// <summary>
/// Service for syncing Trakt watchlists and automatically adding items to download systems.
/// </summary>
public class WatchlistSyncService
{
    private readonly ILogger<WatchlistSyncService> _logger;
    private readonly TraktApi _traktApi;
    private readonly LocalLibraryService _localLibrary;
    private readonly DownloadProviderFactory _downloadProviderFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchlistSyncService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="traktApi">The Trakt API service.</param>
    /// <param name="localLibrary">The local library service.</param>
    /// <param name="downloadProviderFactory">The download provider factory.</param>
    public WatchlistSyncService(
        ILogger<WatchlistSyncService> logger,
        TraktApi traktApi,
        LocalLibraryService localLibrary,
        DownloadProviderFactory downloadProviderFactory)
    {
        _logger = logger;
        _traktApi = traktApi;
        _localLibrary = localLibrary;
        _downloadProviderFactory = downloadProviderFactory;
    }

    /// <summary>
    /// Syncs watchlists for all users with watchlist sync enabled.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SyncAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting watchlist sync for all users");

        var config = Plugin.Instance?.Configuration;
        if (config == null)
        {
            _logger.LogError("Plugin configuration not available");
            return;
        }

        var traktUsers = config.GetAllTraktUsers();
        if (traktUsers.Count == 0)
        {
            _logger.LogInformation("No Trakt users configured, skipping watchlist sync");
            return;
        }

        var syncTasks = new List<Task>();
        foreach (var traktUser in traktUsers)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // Check if watchlist sync is enabled for this user
            if (!traktUser.SyncWatchlistMovies && !traktUser.SyncWatchlistShows)
            {
                continue;
            }

            syncTasks.Add(SyncUserAsync(traktUser.LinkedMbUserId, cancellationToken));
        }

        await Task.WhenAll(syncTasks);

        _logger.LogInformation("Completed watchlist sync for all users");
    }

    /// <summary>
    /// Syncs watchlist for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SyncUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting watchlist sync for user {UserId}", userId);

        var traktUser = UserHelper.GetTraktUser(userId);
        if (traktUser == null)
        {
            _logger.LogWarning("No Trakt user found for {UserId}", userId);
            return;
        }

        var addedMovies = 0;
        var addedShows = 0;

        try
        {
            // Sync movies
            if (traktUser.SyncWatchlistMovies)
            {
                addedMovies = await SyncMovieWatchlistAsync(traktUser, cancellationToken);
            }

            // Sync shows
            if (traktUser.SyncWatchlistShows)
            {
                addedShows = await SyncShowWatchlistAsync(traktUser, cancellationToken);
            }

            // Save updated processed IDs
            Plugin.Instance?.SaveConfiguration();

            _logger.LogInformation(
                "Completed watchlist sync for user {UserId}: {AddedMovies} movies, {AddedShows} shows added",
                userId,
                addedMovies,
                addedShows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing watchlist for user {UserId}", userId);
        }
    }

    private async Task<int> SyncMovieWatchlistAsync(
        Models.Trakt.TraktUser traktUser,
        CancellationToken cancellationToken = default)
    {
        var addedCount = 0;

        try
        {
            var watchlistMovies = await _traktApi.GetMovieWatchlist(traktUser);
            _logger.LogInformation(
                "Found {Count} movies in watchlist for user {UserId}",
                watchlistMovies.Length,
                traktUser.LinkedMbUserId);

            if (watchlistMovies.Length > 0)
            {
                _logger.LogInformation(
                    "Watchlist movies: {Movies}",
                    string.Join(", ", watchlistMovies.Select(m => $"{m.Title} ({m.Year}) [TMDB: {m.Ids.Tmdb}]")));
            }

            var downloadProvider = _downloadProviderFactory.GetProvider();

            foreach (var movie in watchlistMovies)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (!movie.Ids.Tmdb.HasValue)
                {
                    _logger.LogWarning(
                        "Skipping movie {Title} ({Year}): No TMDB ID available",
                        movie.Title,
                        movie.Year);
                    continue;
                }

                var tmdbId = movie.Ids.Tmdb.Value;

                // Check if already in library
                if (_localLibrary.DoesMovieExist(tmdbId))
                {
                    _logger.LogInformation(
                        "Skipping movie {Title} ({Year}): Already in library",
                        movie.Title,
                        movie.Year);
                    continue;
                }

                // Add to download system
                _logger.LogInformation(
                    "Adding movie {Title} ({Year}) to download queue",
                    movie.Title,
                    movie.Year);

                var contentItem = new ContentItem
                {
                    Type = ContentType.Movie,
                    Title = movie.Title,
                    Year = movie.Year,
                    TmdbId = tmdbId,
                    ImdbId = movie.Ids.Imdb,
                    TraktId = movie.Ids.Trakt,
                    ProviderName = "Watchlist",
                    Genres = movie.Genres
                };

                var result = await downloadProvider.RequestMovieAsync(contentItem, traktUser.LinkedMbUserId.ToString());

                if (result.Success)
                {
                    _logger.LogInformation(
                        "Successfully added movie {Title} ({Year}): {Message}",
                        movie.Title,
                        movie.Year,
                        result.Message);
                    addedCount++;
                }
                else
                {
                    _logger.LogError(
                        "Failed to add movie {Title} ({Year}): {Message}",
                        movie.Title,
                        movie.Year,
                        result.Message);
                }

                // Small delay to avoid overwhelming the download system
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing movie watchlist for user {UserId}", traktUser.LinkedMbUserId);
        }

        return addedCount;
    }

    private async Task<int> SyncShowWatchlistAsync(
        Models.Trakt.TraktUser traktUser,
        CancellationToken cancellationToken = default)
    {
        var addedCount = 0;

        try
        {
            var watchlistShows = await _traktApi.GetShowWatchlist(traktUser);
            _logger.LogInformation(
                "Found {Count} shows in watchlist for user {UserId}",
                watchlistShows.Length,
                traktUser.LinkedMbUserId);

            if (watchlistShows.Length > 0)
            {
                _logger.LogInformation(
                    "Watchlist shows: {Shows}",
                    string.Join(", ", watchlistShows.Select(s => $"{s.Title} ({s.Year}) [TVDB: {s.Ids.Tvdb}]")));
            }

            var downloadProvider = _downloadProviderFactory.GetProvider();

            foreach (var show in watchlistShows)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (!show.Ids.Tvdb.HasValue)
                {
                    _logger.LogWarning(
                        "Skipping show {Title} ({Year}): No TVDB ID available",
                        show.Title,
                        show.Year);
                    continue;
                }

                var tvdbId = show.Ids.Tvdb.Value;

                // Check if already in library (any season)
                var existingSeries = _localLibrary.FindSeriesByTvdbId(tvdbId);
                if (existingSeries != null)
                {
                    _logger.LogInformation(
                        "Skipping show {Title} ({Year}): Already in library",
                        show.Title,
                        show.Year);
                    continue;
                }

                // Add to download system (Season 1 by default, as Trakt doesn't specify specific seasons in watchlist)
                _logger.LogInformation(
                    "Adding show {Title} ({Year}) Season 1 to download queue",
                    show.Title,
                    show.Year);

                var contentItem = new ContentItem
                {
                    Type = ContentType.Show,
                    Title = show.Title,
                    Year = show.Year,
                    TvdbId = tvdbId,
                    TmdbId = show.Ids.Tmdb,
                    ImdbId = show.Ids.Imdb,
                    TraktId = show.Ids.Trakt,
                    SeasonNumber = 1,
                    ProviderName = "Watchlist",
                    Genres = show.Genres
                };

                // Check if this is anime (for routing to anime folder/profile)
                var isAnime = show.Genres?.Any(g => g.Equals("anime", StringComparison.OrdinalIgnoreCase)) == true;

                var result = await downloadProvider.RequestShowAsync(
                    contentItem,
                    seasonNumber: 1,
                    playerId: traktUser.LinkedMbUserId.ToString(),
                    isAnime: isAnime);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "Successfully added show {Title} ({Year}) Season 1: {Message}",
                        show.Title,
                        show.Year,
                        result.Message);
                    addedCount++;
                }
                else
                {
                    _logger.LogError(
                        "Failed to add show {Title} ({Year}) Season 1: {Message}",
                        show.Title,
                        show.Year,
                        result.Message);
                }

                // Small delay to avoid overwhelming the download system
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing show watchlist for user {UserId}", traktUser.LinkedMbUserId);
        }

        return addedCount;
    }
}
