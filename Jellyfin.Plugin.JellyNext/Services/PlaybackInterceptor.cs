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
    private readonly ContentCacheService _cacheService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackInterceptor"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="sessionManager">The session manager.</param>
    /// <param name="radarrService">The Radarr service.</param>
    /// <param name="cacheService">The content cache service.</param>
    public PlaybackInterceptor(
        ILogger<PlaybackInterceptor> logger,
        ISessionManager sessionManager,
        RadarrService radarrService,
        ContentCacheService cacheService)
    {
        _logger = logger;
        _sessionManager = sessionManager;
        _radarrService = radarrService;
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
            if (e.Item == null || string.IsNullOrEmpty(e.Item.Path))
            {
                return;
            }

            // Check if this is a JellyNext virtual item
            if (!e.Item.Path.Contains("jellynext-virtual", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _logger.LogInformation("Detected playback attempt for virtual item: {Path}", e.Item.Path);

            // Extract TMDB ID from path
            var fileName = System.IO.Path.GetFileNameWithoutExtension((string?)e.Item.Path) ?? string.Empty;
            var tmdbMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"\[tmdbid-(\d+)\]$");

            if (!tmdbMatch.Success || !int.TryParse(tmdbMatch.Groups[1].Value, out var tmdbId))
            {
                _logger.LogWarning("Could not extract TMDB ID from virtual item path: {Path}", e.Item.Path);
                return;
            }

            // Extract userId from path
            var pathMatch = System.Text.RegularExpressions.Regex.Match(
                e.Item.Path,
                @"jellynext-virtual[/\\]([a-f0-9-]+)[/\\]");

            if (!pathMatch.Success || !Guid.TryParse(pathMatch.Groups[1].Value, out var itemUserId))
            {
                _logger.LogWarning("Could not extract user ID from virtual item path: {Path}", e.Item.Path);
                itemUserId = e.Session.UserId;
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error intercepting playback for virtual item");
        }
    }
}
