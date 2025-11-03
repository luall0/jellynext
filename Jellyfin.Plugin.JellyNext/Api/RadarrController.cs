using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Models;
using Jellyfin.Plugin.JellyNext.Services;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Api;

/// <summary>
/// API controller for Radarr operations.
/// </summary>
[ApiController]
[Route("JellyNext/Radarr")]
[Produces("application/json")]
public class RadarrController : ControllerBase
{
    private readonly ILogger<RadarrController> _logger;
    private readonly RadarrService _radarrService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RadarrController"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="radarrService">The Radarr service.</param>
    public RadarrController(ILogger<RadarrController> logger, RadarrService radarrService)
    {
        _logger = logger;
        _radarrService = radarrService;
    }

    /// <summary>
    /// Tests the connection to Radarr and retrieves available profiles and folders.
    /// </summary>
    /// <param name="radarrUrl">The Radarr URL.</param>
    /// <param name="apiKey">The Radarr API key.</param>
    /// <returns>Connection test result with profiles and folders.</returns>
    [HttpGet("TestConnection")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RadarrTestConnectionResponse>> TestConnection(
        [FromQuery] [Required] string radarrUrl,
        [FromQuery] [Required] string apiKey)
    {
        _logger.LogInformation("Testing Radarr connection to {Url}", radarrUrl);

        var result = await _radarrService.TestConnectionAsync(radarrUrl, apiKey);

        if (!result.Success)
        {
            _logger.LogWarning("Radarr connection test failed: {Error}", result.ErrorMessage);
        }

        return Ok(result);
    }
}
