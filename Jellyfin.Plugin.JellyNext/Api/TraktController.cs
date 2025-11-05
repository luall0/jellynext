using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Helpers;
using Jellyfin.Plugin.JellyNext.Services;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Api;

/// <summary>
/// API controller for Trakt OAuth operations.
/// </summary>
[ApiController]
[Route("JellyNext/Trakt")]
[Produces("application/json")]
public class TraktController : ControllerBase
{
    private readonly ILogger<TraktController> _logger;
    private readonly TraktApi _traktApi;

    /// <summary>
    /// Initializes a new instance of the <see cref="TraktController"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="traktApi">The Trakt API service.</param>
    public TraktController(ILogger<TraktController> logger, TraktApi traktApi)
    {
        _logger = logger;
        _traktApi = traktApi;
    }

    /// <summary>
    /// Initiates OAuth device authorization for a user.
    /// </summary>
    /// <param name="userGuid">The Jellyfin user GUID.</param>
    /// <returns>The user code and verification URL.</returns>
    [HttpPost("Users/{userGuid}/Authorize")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> AuthorizeUser([FromRoute][Required] Guid userGuid)
    {
        try
        {
            var traktUser = UserHelper.GetTraktUser(userGuid);
            if (traktUser == null)
            {
                Plugin.Instance?.Configuration.AddUser(userGuid);
                Plugin.Instance?.SaveConfiguration();
                traktUser = UserHelper.GetTraktUser(userGuid);
            }

            if (traktUser == null)
            {
                return BadRequest(new { error = "Failed to create Trakt user configuration" });
            }

            var userCode = await _traktApi.AuthorizeDevice(traktUser);

            return Ok(new
            {
                userCode,
                verificationUrl = "https://trakt.tv/activate"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating Trakt authorization for user {UserGuid}", userGuid);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Checks the authorization status for a user.
    /// </summary>
    /// <param name="userGuid">The Jellyfin user GUID.</param>
    /// <returns>The authorization status.</returns>
    [HttpGet("Users/{userGuid}/AuthorizationStatus")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> GetAuthorizationStatus([FromRoute][Required] Guid userGuid)
    {
        var traktUser = UserHelper.GetTraktUser(userGuid);
        var isAuthorized = traktUser != null &&
                          !string.IsNullOrEmpty(traktUser.AccessToken) &&
                          !string.IsNullOrEmpty(traktUser.RefreshToken);

        return Ok(new { isAuthorized });
    }

    /// <summary>
    /// Deauthorizes a user's Trakt account.
    /// </summary>
    /// <param name="userGuid">The Jellyfin user GUID.</param>
    /// <returns>Success status.</returns>
    [HttpPost("Users/{userGuid}/Deauthorize")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult DeauthorizeUser([FromRoute][Required] Guid userGuid)
    {
        var traktUser = UserHelper.GetTraktUser(userGuid);
        if (traktUser == null)
        {
            return NotFound(new { error = "Trakt user configuration not found" });
        }

        Plugin.Instance?.Configuration.RemoveUser(userGuid);
        Plugin.Instance?.SaveConfiguration();

        _logger.LogInformation("Deauthorized Trakt for user {UserGuid}", userGuid);

        return Ok(new { success = true });
    }
}
