using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Models.Common;
using Jellyfin.Plugin.JellyNext.Models.Radarr;
using Jellyfin.Plugin.JellyNext.Models.Sonarr;
using Jellyfin.Plugin.JellyNext.Models.Trakt;
using Jellyfin.Plugin.JellyNext.Services;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Api;

/// <summary>
/// API controller for Sonarr operations.
/// </summary>
[ApiController]
[Route("JellyNext/Sonarr")]
[Produces("application/json")]
public class SonarrController : ControllerBase
{
    private readonly ILogger<SonarrController> _logger;
    private readonly SonarrService _sonarrService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SonarrController"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="sonarrService">The Sonarr service.</param>
    public SonarrController(ILogger<SonarrController> logger, SonarrService sonarrService)
    {
        _logger = logger;
        _sonarrService = sonarrService;
    }

    /// <summary>
    /// Tests the connection to Sonarr and retrieves available profiles and folders.
    /// </summary>
    /// <param name="sonarrUrl">The Sonarr URL.</param>
    /// <param name="apiKey">The Sonarr API key.</param>
    /// <returns>Connection test result with profiles and folders.</returns>
    [HttpGet("TestConnection")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SonarrTestConnectionResponse>> TestConnection(
        [FromQuery][Required] string sonarrUrl,
        [FromQuery][Required] string apiKey)
    {
        _logger.LogInformation("Testing Sonarr connection to {Url}", sonarrUrl);

        var result = await _sonarrService.TestConnectionAsync(sonarrUrl, apiKey);

        if (!result.Success)
        {
            _logger.LogWarning("Sonarr connection test failed: {Error}", result.ErrorMessage);
        }

        return Ok(result);
    }
}
