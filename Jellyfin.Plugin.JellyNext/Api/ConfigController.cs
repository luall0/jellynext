using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.JellyNext.Api;

/// <summary>
/// Configuration file server controller.
/// Serves configuration page resources (HTML, JS, CSS) from embedded resources.
/// </summary>
[ApiController]
[Route("JellyNext/Config")]
public class ConfigController : ControllerBase
{
    /// <summary>
    /// Gets a tab's HTML content.
    /// </summary>
    /// <param name="tabName">The tab name (general, trakt, trending, downloads).</param>
    /// <returns>The HTML content of the tab.</returns>
    [HttpGet("Tab/{tabName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult GetTabHtml(string tabName)
    {
        try
        {
            var resourcePath = $"Jellyfin.Plugin.JellyNext.Configuration.tabs.{tabName}.html";
            var content = GetEmbeddedResource(resourcePath);

            if (content == null)
            {
                return NotFound($"Tab HTML not found: {tabName}");
            }

            return Content(content, "text/html");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Gets a tab's JavaScript content.
    /// </summary>
    /// <param name="tabName">The tab name (general, trakt, trending, downloads).</param>
    /// <returns>The JavaScript content of the tab.</returns>
    [HttpGet("Tab/{tabName}/js")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult GetTabJs(string tabName)
    {
        try
        {
            var resourcePath = $"Jellyfin.Plugin.JellyNext.Configuration.tabs.{tabName}.js";
            var content = GetEmbeddedResource(resourcePath);

            if (content == null)
            {
                return NotFound($"Tab JavaScript not found: {tabName}");
            }

            return Content(content, "application/javascript");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Reads an embedded resource from the assembly.
    /// </summary>
    /// <param name="resourcePath">The full resource path.</param>
    /// <returns>The resource content as a string, or null if not found.</returns>
    private static string? GetEmbeddedResource(string resourcePath)
    {
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream == null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
