using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.Services;

/// <summary>
/// Service for accessing TMDB metadata.
/// </summary>
public class TmdbService
{
    private readonly ILogger<TmdbService> _logger;
    private string? _cachedJellyfinApiKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="TmdbService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public TmdbService(ILogger<TmdbService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the TMDB API key to use (custom or Jellyfin's default).
    /// </summary>
    /// <returns>The TMDB API key.</returns>
    public string GetTmdbApiKey()
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null)
        {
            throw new InvalidOperationException("Plugin configuration is not available");
        }

        // Use custom key if provided
        if (!string.IsNullOrWhiteSpace(config.TmdbApiKey))
        {
            _logger.LogDebug("Using custom TMDB API key from configuration");
            return config.TmdbApiKey;
        }

        // Fall back to Jellyfin's API key via reflection
        if (_cachedJellyfinApiKey != null)
        {
            _logger.LogDebug("Using cached Jellyfin TMDB API key");
            return _cachedJellyfinApiKey;
        }

        try
        {
            // Try to get Jellyfin's TMDB plugin configuration via reflection
            var jellyfinPluginManager = GetJellyfinPluginManager();
            if (jellyfinPluginManager != null)
            {
                var tmdbPlugin = GetTmdbPlugin(jellyfinPluginManager);
                if (tmdbPlugin != null)
                {
                    var apiKey = ExtractApiKeyFromPlugin(tmdbPlugin);
                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        _cachedJellyfinApiKey = apiKey;
                        _logger.LogInformation("Successfully retrieved Jellyfin's TMDB API key via reflection");
                        return apiKey;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve Jellyfin's TMDB API key via reflection");
        }

        throw new InvalidOperationException(
            "No TMDB API key available. Please provide a custom TMDB API key in the plugin configuration.");
    }

    private object? GetJellyfinPluginManager()
    {
        try
        {
            // Get the plugin manager from MediaBrowser.Common assembly
            var commonAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "MediaBrowser.Common");

            if (commonAssembly == null)
            {
                return null;
            }

            var pluginManagerType = commonAssembly.GetType("MediaBrowser.Common.Plugins.IPluginManager");
            if (pluginManagerType == null)
            {
                return null;
            }

            // Get plugin manager instance from application host
            var applicationHost = Plugin.Instance?.ApplicationHost;
            if (applicationHost == null)
            {
                return null;
            }

            var pluginManagerProperty = applicationHost.GetType()
                .GetProperty("PluginManager", BindingFlags.Public | BindingFlags.Instance);

            return pluginManagerProperty?.GetValue(applicationHost);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get Jellyfin plugin manager");
            return null;
        }
    }

    private object? GetTmdbPlugin(object pluginManager)
    {
        try
        {
            var pluginsProperty = pluginManager.GetType()
                .GetProperty("Plugins", BindingFlags.Public | BindingFlags.Instance);

            if (pluginsProperty?.GetValue(pluginManager) is not System.Collections.IEnumerable plugins)
            {
                return null;
            }

            foreach (var plugin in plugins)
            {
                var pluginType = plugin.GetType();
                if (pluginType.FullName?.Contains("Tmdb", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return plugin;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get TMDB plugin");
        }

        return null;
    }

    private string? ExtractApiKeyFromPlugin(object tmdbPlugin)
    {
        try
        {
            // Try to get Configuration property
            var configProperty = tmdbPlugin.GetType()
                .GetProperty("Configuration", BindingFlags.Public | BindingFlags.Instance);

            var config = configProperty?.GetValue(tmdbPlugin);
            if (config == null)
            {
                return null;
            }

            // Try to get TmdbApiKey property from configuration
            var apiKeyProperty = config.GetType()
                .GetProperty("TmdbApiKey", BindingFlags.Public | BindingFlags.Instance);

            return apiKeyProperty?.GetValue(config) as string;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract API key from TMDB plugin");
            return null;
        }
    }
}
