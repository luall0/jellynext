using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Models;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Services;

/// <summary>
/// Service that intercepts playback of virtual items and triggers downloads.
/// </summary>
public class PlaybackInterceptor : IHostedService
{
    private readonly ILogger<PlaybackInterceptor> _logger;
    private readonly ISessionManager _sessionManager;
    private readonly RadarrService _radarrService;
    private readonly SonarrService _sonarrService;
    private readonly ContentCacheService _cacheService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackInterceptor"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="sessionManager">The session manager.</param>
    /// <param name="radarrService">The Radarr service.</param>
    /// <param name="sonarrService">The Sonarr service.</param>
    /// <param name="cacheService">The content cache service.</param>
    public PlaybackInterceptor(
        ILogger<PlaybackInterceptor> logger,
        ISessionManager sessionManager,
        RadarrService radarrService,
        SonarrService sonarrService,
        ContentCacheService cacheService)
    {
        _logger = logger;
        _sessionManager = sessionManager;
        _radarrService = radarrService;
        _sonarrService = sonarrService;
        _cacheService = cacheService;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart += OnPlaybackStart;
        _logger.LogInformation("PlaybackInterceptor started - monitoring for virtual item playback");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart -= OnPlaybackStart;
        _logger.LogInformation("PlaybackInterceptor stopped");
        return Task.CompletedTask;
    }

    private async void OnPlaybackStart(object? sender, PlaybackProgressEventArgs e)
    {
        try
        {
            if (e.Item == null || string.IsNullOrEmpty(e.Item.Path) || e.Session == null)
            {
                return;
            }

            // Check if this is a JellyNext virtual item
            if (!e.Item.Path.Contains("jellynext-virtual", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _logger.LogInformation("Detected playback attempt for virtual item: {Path}", e.Item.Path);

            // Extract userId from path
            var pathMatch = System.Text.RegularExpressions.Regex.Match(
                e.Item.Path,
                @"jellynext-virtual[/\\]([a-f0-9-]+)[/\\]");

            if (!pathMatch.Success || !Guid.TryParse(pathMatch.Groups[1].Value, out var itemUserId))
            {
                _logger.LogWarning("Could not extract user ID from virtual item path: {Path}", e.Item.Path);
                itemUserId = e.Session.UserId;
            }

            // Stop playback immediately
            try
            {
                await _sessionManager.SendPlaystateCommand(
                    null,
                    e.Session.Id,
                    new MediaBrowser.Model.Session.PlaystateRequest
                    {
                        Command = MediaBrowser.Model.Session.PlaystateCommand.Stop,
                        ControllingUserId = e.Session.UserId.ToString()
                    },
                    CancellationToken.None);
            }
            catch (Exception stopEx)
            {
                _logger.LogWarning(stopEx, "Could not stop playback session");
            }

            // Determine if this is a movie or show based on path
            if (e.Item.Path.Contains("movies_", StringComparison.OrdinalIgnoreCase))
            {
                await HandleMovieDownload(e, itemUserId);
            }
            else if (e.Item.Path.Contains("shows_", StringComparison.OrdinalIgnoreCase))
            {
                await HandleShowDownload(e, itemUserId);
            }
            else
            {
                _logger.LogWarning("Unknown content type in virtual item path: {Path}", e.Item.Path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error intercepting playback for virtual item");
        }
    }

    private async Task HandleMovieDownload(PlaybackProgressEventArgs e, Guid itemUserId)
    {
        // Extract TMDB ID from filename
        var fileName = System.IO.Path.GetFileNameWithoutExtension((string?)e.Item?.Path) ?? string.Empty;
        var tmdbMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"\[tmdbid-(\d+)\]$");

        if (!tmdbMatch.Success || !int.TryParse(tmdbMatch.Groups[1].Value, out var tmdbId))
        {
            _logger.LogWarning("Could not extract TMDB ID from movie path: {Path}", e.Item?.Path);
            return;
        }

        // Get movie details from cache
        var cachedContent = _cacheService.GetCachedContent(itemUserId, "recommendations");
        var contentItem = cachedContent.FirstOrDefault(c => c.Type == ContentType.Movie && c.TmdbId == tmdbId);

        if (contentItem == null)
        {
            _logger.LogWarning("Movie not found in cache: TMDB ID {TmdbId}", tmdbId);
            return;
        }

        _logger.LogInformation(
            "Triggering download for movie: {Title} ({Year}) - TMDB: {TmdbId}",
            contentItem.Title,
            contentItem.Year,
            tmdbId);

        // Add movie to Radarr
        var result = await _radarrService.AddMovieAsync(
            tmdbId,
            contentItem.Title,
            contentItem.Year ?? DateTime.UtcNow.Year);

        // Send message to user
        var message = result != null
            ? $"{contentItem.Title} ({contentItem.Year}) has been added to your download queue and will appear in your library shortly."
            : $"Failed to add {contentItem.Title} ({contentItem.Year}) to download queue. Please check your Radarr configuration.";

        await SendUserNotification(e.Session?.Id, message);

        if (result != null)
        {
            _logger.LogInformation(
                "Successfully added movie to Radarr: {Title} (TMDB: {TmdbId})",
                contentItem.Title,
                tmdbId);
        }
        else
        {
            _logger.LogError(
                "Failed to add movie to Radarr: {Title} (TMDB: {TmdbId})",
                contentItem.Title,
                tmdbId);
        }
    }

    private async Task HandleShowDownload(PlaybackProgressEventArgs e, Guid itemUserId)
    {
        var path = e.Item?.Path ?? string.Empty;

        // Extract season number from filename (format: S##E## - Download Season #.strm)
        var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
        var seasonMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"S(\d+)E\d+");

        if (!seasonMatch.Success || !int.TryParse(seasonMatch.Groups[1].Value, out var seasonNumber))
        {
            _logger.LogWarning("Could not extract season number from show path: {Path}", path);
            return;
        }

        // Extract TVDB ID from parent folder name (format: Title (Year) [tvdbid-XXXXX])
        var parentFolderName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path));
        var tvdbMatch = System.Text.RegularExpressions.Regex.Match(parentFolderName ?? string.Empty, @"\[tvdbid-(\d+)\]$");

        if (!tvdbMatch.Success || !int.TryParse(tvdbMatch.Groups[1].Value, out var tvdbId))
        {
            _logger.LogWarning("Could not extract TVDB ID from show folder: {Folder}", parentFolderName);
            return;
        }

        // Get show details from cache
        var cachedContent = _cacheService.GetCachedContent(itemUserId, "recommendations");
        var contentItem = cachedContent.FirstOrDefault(c => c.Type == ContentType.Show && c.TvdbId == tvdbId);

        if (contentItem == null)
        {
            _logger.LogWarning("Show not found in cache: TVDB ID {TvdbId}", tvdbId);
            return;
        }

        // Detect if this is anime (simple heuristic for now - can be enhanced later)
        var isAnime = DetectAnime(contentItem);

        _logger.LogInformation(
            "Triggering download for show: {Title} ({Year}) - Season {Season} - TVDB: {TvdbId} - Type: {Type}",
            contentItem.Title,
            contentItem.Year,
            seasonNumber,
            tvdbId,
            isAnime ? "Anime" : "Standard");

        // Add series to Sonarr with specific season monitored
        var result = await _sonarrService.AddSeriesAsync(
            tvdbId,
            contentItem.Title,
            contentItem.Year,
            seasonNumber,
            isAnime);

        // Send message to user
        var message = result != null
            ? $"{contentItem.Title} ({contentItem.Year}) - Season {seasonNumber} has been added to your download queue and will appear in your library shortly."
            : $"Failed to add {contentItem.Title} ({contentItem.Year}) - Season {seasonNumber} to download queue. Please check your Sonarr configuration.";

        await SendUserNotification(e.Session?.Id, message);

        if (result != null)
        {
            _logger.LogInformation(
                "Successfully added show to Sonarr: {Title} - Season {Season} (TVDB: {TvdbId})",
                contentItem.Title,
                seasonNumber,
                tvdbId);
        }
        else
        {
            _logger.LogError(
                "Failed to add show to Sonarr: {Title} - Season {Season} (TVDB: {TvdbId})",
                contentItem.Title,
                seasonNumber,
                tvdbId);
        }
    }

    private bool DetectAnime(ContentItem item)
    {
        // Simple anime detection heuristic
        // Can be enhanced later with:
        // - Genre information from Trakt
        // - TVDB tags
        // - User configuration
        // - Machine learning based on title patterns

        var title = item.Title?.ToLowerInvariant() ?? string.Empty;

        // Common anime indicators in titles
        var animeKeywords = new[]
        {
            "anime",
            "shippuden",
            "no hero academia",
            "attack on titan",
            "dragon ball",
            "one piece",
            "naruto",
            "bleach",
            "demon slayer",
            "death note"
        };

        return animeKeywords.Any(keyword => title.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private async Task SendUserNotification(string? sessionId, string message)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            return;
        }

        try
        {
            await _sessionManager.SendMessageCommand(
                null,
                sessionId,
                new MediaBrowser.Model.Session.MessageCommand
                {
                    Header = "JellyNext Download",
                    Text = message,
                    TimeoutMs = 5000
                },
                CancellationToken.None);
        }
        catch (Exception msgEx)
        {
            _logger.LogWarning(msgEx, "Could not send message to session");
        }
    }
}
