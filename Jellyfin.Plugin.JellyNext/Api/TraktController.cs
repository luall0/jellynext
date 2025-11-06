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

    /// <summary>
    /// Gets the user-specific Trakt settings.
    /// </summary>
    /// <param name="userGuid">The Jellyfin user GUID.</param>
    /// <returns>The user settings.</returns>
    [HttpGet("Users/{userGuid}/Settings")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<object> GetUserSettings([FromRoute][Required] Guid userGuid)
    {
        var traktUser = UserHelper.GetTraktUser(userGuid);
        if (traktUser == null)
        {
            return NotFound(new { error = "Trakt user configuration not found" });
        }

        return Ok(new
        {
            syncMovieRecommendations = traktUser.SyncMovieRecommendations,
            syncShowRecommendations = traktUser.SyncShowRecommendations,
            syncNextSeasons = traktUser.SyncNextSeasons,
            ignoreCollected = traktUser.IgnoreCollected,
            ignoreWatchlisted = traktUser.IgnoreWatchlisted,
            limitShowsToSeasonOne = traktUser.LimitShowsToSeasonOne
        });
    }

    /// <summary>
    /// Updates the user-specific Trakt settings.
    /// </summary>
    /// <param name="userGuid">The Jellyfin user GUID.</param>
    /// <param name="settings">The updated settings.</param>
    /// <returns>Success status.</returns>
    [HttpPost("Users/{userGuid}/Settings")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult UpdateUserSettings([FromRoute][Required] Guid userGuid, [FromBody] UserSettingsDto settings)
    {
        var traktUser = UserHelper.GetTraktUser(userGuid);
        if (traktUser == null)
        {
            return NotFound(new { error = "Trakt user configuration not found" });
        }

        traktUser.SyncMovieRecommendations = settings.SyncMovieRecommendations;
        traktUser.SyncShowRecommendations = settings.SyncShowRecommendations;
        traktUser.SyncNextSeasons = settings.SyncNextSeasons;
        traktUser.IgnoreCollected = settings.IgnoreCollected;
        traktUser.IgnoreWatchlisted = settings.IgnoreWatchlisted;
        traktUser.LimitShowsToSeasonOne = settings.LimitShowsToSeasonOne;

        Plugin.Instance?.SaveConfiguration();

        _logger.LogInformation("Updated Trakt settings for user {UserGuid}", userGuid);

        return Ok(new { success = true });
    }

    /// <summary>
    /// DTO for user settings update.
    /// </summary>
    public class UserSettingsDto
    {
        /// <summary>
        /// Gets or sets a value indicating whether to sync movie recommendations.
        /// </summary>
        public bool SyncMovieRecommendations { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to sync show recommendations.
        /// </summary>
        public bool SyncShowRecommendations { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to sync next seasons.
        /// </summary>
        public bool SyncNextSeasons { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore collected items.
        /// </summary>
        public bool IgnoreCollected { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore watchlisted items.
        /// </summary>
        public bool IgnoreWatchlisted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to limit shows to season 1 only.
        /// </summary>
        public bool LimitShowsToSeasonOne { get; set; }
    }
}
