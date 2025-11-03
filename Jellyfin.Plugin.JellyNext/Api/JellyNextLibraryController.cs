using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Jellyfin.Plugin.JellyNext.Models;
using Jellyfin.Plugin.JellyNext.Services;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Api;

/// <summary>
/// API controller for JellyNext virtual library.
/// </summary>
[ApiController]
[Route("[controller]")]
public class JellyNextLibraryController : ControllerBase
{
    private readonly ILogger<JellyNextLibraryController> _logger;
    private readonly ContentCacheService _cacheService;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyNextLibraryController"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="cacheService">The content cache service.</param>
    public JellyNextLibraryController(
        ILogger<JellyNextLibraryController> logger,
        ContentCacheService cacheService)
    {
        _logger = logger;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Gets movie recommendations for the admin user.
    /// </summary>
    /// <param name="startIndex">Optional start index for paging.</param>
    /// <param name="limit">Optional limit for paging.</param>
    /// <returns>Query result with movie recommendations.</returns>
    [HttpGet("Recommendations/Movies")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<QueryResult<ContentItem>> GetMovieRecommendations(
        [FromQuery] int? startIndex,
        [FromQuery] int? limit)
    {
        try
        {
            // Get admin user (first Trakt user for now)
            var config = Plugin.Instance?.Configuration;
            if (config?.TraktUsers == null || config.TraktUsers.Length == 0)
            {
                _logger.LogWarning("No Trakt users configured");
                return NotFound(new { error = "No Trakt users configured" });
            }

            var adminUser = config.TraktUsers[0];
            var userId = adminUser.LinkedMbUserId;

            // Get cached recommendations
            var cachedContent = _cacheService.GetCachedContent(userId, "recommendations");
            var movies = cachedContent.Where(c => c.Type == ContentType.Movie).ToList();

            // Apply paging
            var totalCount = movies.Count;
            if (startIndex.HasValue)
            {
                movies = movies.Skip(startIndex.Value).ToList();
            }

            if (limit.HasValue)
            {
                movies = movies.Take(limit.Value).ToList();
            }

            _logger.LogDebug("Returning {Count} movie recommendations (total: {Total})", movies.Count, totalCount);

            return Ok(new QueryResult<ContentItem>
            {
                Items = movies,
                TotalRecordCount = totalCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving movie recommendations");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets all recommendations (movies and shows) for the admin user.
    /// </summary>
    /// <param name="startIndex">Optional start index for paging.</param>
    /// <param name="limit">Optional limit for paging.</param>
    /// <returns>Query result with all recommendations.</returns>
    [HttpGet("Recommendations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<QueryResult<ContentItem>> GetAllRecommendations(
        [FromQuery] int? startIndex,
        [FromQuery] int? limit)
    {
        try
        {
            // Get admin user (first Trakt user for now)
            var config = Plugin.Instance?.Configuration;
            if (config?.TraktUsers == null || config.TraktUsers.Length == 0)
            {
                _logger.LogWarning("No Trakt users configured");
                return NotFound(new { error = "No Trakt users configured" });
            }

            var adminUser = config.TraktUsers[0];
            var userId = adminUser.LinkedMbUserId;

            // Get cached recommendations
            var cachedContent = _cacheService.GetCachedContent(userId, "recommendations").ToList();

            // Apply paging
            var totalCount = cachedContent.Count;
            if (startIndex.HasValue)
            {
                cachedContent = cachedContent.Skip(startIndex.Value).ToList();
            }

            if (limit.HasValue)
            {
                cachedContent = cachedContent.Take(limit.Value).ToList();
            }

            _logger.LogDebug("Returning {Count} recommendations (total: {Total})", cachedContent.Count, totalCount);

            return Ok(new QueryResult<ContentItem>
            {
                Items = cachedContent,
                TotalRecordCount = totalCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recommendations");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a specific content item by TMDB ID.
    /// </summary>
    /// <param name="tmdbId">The TMDB ID.</param>
    /// <returns>The content item if found.</returns>
    [HttpGet("Item/{tmdbId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ContentItem> GetItem([FromRoute, Required] int tmdbId)
    {
        try
        {
            // Get admin user (first Trakt user for now)
            var config = Plugin.Instance?.Configuration;
            if (config?.TraktUsers == null || config.TraktUsers.Length == 0)
            {
                _logger.LogWarning("No Trakt users configured");
                return NotFound(new { error = "No Trakt users configured" });
            }

            var adminUser = config.TraktUsers[0];
            var userId = adminUser.LinkedMbUserId;

            // Get cached recommendations
            var cachedContent = _cacheService.GetCachedContent(userId, "recommendations");
            var item = cachedContent.FirstOrDefault(c => c.TmdbId == tmdbId);

            if (item == null)
            {
                _logger.LogWarning("Item not found: TMDB ID {TmdbId}", tmdbId);
                return NotFound(new { error = $"Item with TMDB ID {tmdbId} not found" });
            }

            return Ok(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving item {TmdbId}", tmdbId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }
}
