using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Models.Jellyseerr;
using Jellyfin.Plugin.JellyNext.Services;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Api;

/// <summary>
/// API controller for Jellyseerr operations.
/// </summary>
[ApiController]
[Route("JellyNext/Jellyseerr")]
[Produces("application/json")]
public class JellyseerrController : ControllerBase
{
    private readonly ILogger<JellyseerrController> _logger;
    private readonly JellyseerrService _jellyseerrService;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyseerrController"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="jellyseerrService">The Jellyseerr service.</param>
    public JellyseerrController(ILogger<JellyseerrController> logger, JellyseerrService jellyseerrService)
    {
        _logger = logger;
        _jellyseerrService = jellyseerrService;
    }

    /// <summary>
    /// Tests the connection to Jellyseerr.
    /// </summary>
    /// <param name="jellyseerrUrl">The Jellyseerr URL.</param>
    /// <param name="apiKey">The Jellyseerr API key.</param>
    /// <returns>Connection test result.</returns>
    [HttpGet("TestConnection")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TestConnectionResponse>> TestConnection(
        [FromQuery][Required] string jellyseerrUrl,
        [FromQuery][Required] string apiKey)
    {
        _logger.LogInformation("Testing Jellyseerr connection to {Url}", jellyseerrUrl);

        var result = await _jellyseerrService.TestConnectionAsync(jellyseerrUrl, apiKey);

        if (!result.Success)
        {
            _logger.LogWarning("Jellyseerr connection test failed: {Error}", result.ErrorMessage);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets all Radarr servers configured in Jellyseerr.
    /// </summary>
    /// <param name="jellyseerrUrl">The Jellyseerr URL.</param>
    /// <param name="apiKey">The Jellyseerr API key.</param>
    /// <returns>List of Radarr servers.</returns>
    [HttpGet("Radarr/Servers")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RadarrServer>>> GetRadarrServers(
        [FromQuery][Required] string jellyseerrUrl,
        [FromQuery][Required] string apiKey)
    {
        _logger.LogInformation("Fetching Radarr servers from Jellyseerr");
        var servers = await _jellyseerrService.GetRadarrServersAsync(jellyseerrUrl, apiKey);
        return Ok(servers ?? new List<RadarrServer>());
    }

    /// <summary>
    /// Gets all Sonarr servers configured in Jellyseerr.
    /// </summary>
    /// <param name="jellyseerrUrl">The Jellyseerr URL.</param>
    /// <param name="apiKey">The Jellyseerr API key.</param>
    /// <returns>List of Sonarr servers.</returns>
    [HttpGet("Sonarr/Servers")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SonarrServer>>> GetSonarrServers(
        [FromQuery][Required] string jellyseerrUrl,
        [FromQuery][Required] string apiKey)
    {
        _logger.LogInformation("Fetching Sonarr servers from Jellyseerr");
        var servers = await _jellyseerrService.GetSonarrServersAsync(jellyseerrUrl, apiKey);
        return Ok(servers ?? new List<SonarrServer>());
    }

    /// <summary>
    /// Gets service details (profiles, root folders) for a specific Radarr server.
    /// </summary>
    /// <param name="jellyseerrUrl">The Jellyseerr URL.</param>
    /// <param name="apiKey">The Jellyseerr API key.</param>
    /// <param name="serverId">The Radarr server ID.</param>
    /// <returns>List of quality profiles.</returns>
    [HttpGet("Radarr/{serverId}/Profiles")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<QualityProfile>>> GetRadarrProfiles(
        [FromQuery][Required] string jellyseerrUrl,
        [FromQuery][Required] string apiKey,
        [FromRoute][Required] int serverId)
    {
        _logger.LogInformation("Fetching Radarr service details for server {ServerId}", serverId);
        var profiles = await _jellyseerrService.GetRadarrProfilesAsync(jellyseerrUrl, apiKey, serverId);
        return Ok(profiles ?? new List<QualityProfile>());
    }

    /// <summary>
    /// Gets service details (profiles, root folders) for a specific Sonarr server.
    /// </summary>
    /// <param name="jellyseerrUrl">The Jellyseerr URL.</param>
    /// <param name="apiKey">The Jellyseerr API key.</param>
    /// <param name="serverId">The Sonarr server ID.</param>
    /// <returns>List of quality profiles.</returns>
    [HttpGet("Sonarr/{serverId}/Profiles")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<QualityProfile>>> GetSonarrProfiles(
        [FromQuery][Required] string jellyseerrUrl,
        [FromQuery][Required] string apiKey,
        [FromRoute][Required] int serverId)
    {
        _logger.LogInformation("Fetching Sonarr service details for server {ServerId}", serverId);
        var profiles = await _jellyseerrService.GetSonarrProfilesAsync(jellyseerrUrl, apiKey, serverId);
        return Ok(profiles ?? new List<QualityProfile>());
    }
}
