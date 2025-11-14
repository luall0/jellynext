using System;
using Jellyfin.Plugin.JellyNext.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.JellyNext.Services.DownloadProviders;

/// <summary>
/// Factory for creating the appropriate download provider based on configuration.
/// </summary>
public class DownloadProviderFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadProviderFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public DownloadProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets the appropriate download provider based on current configuration.
    /// </summary>
    /// <returns>The configured download provider.</returns>
    public IDownloadProvider GetProvider()
    {
        var config = Plugin.Instance?.Configuration;
        var integrationType = config?.DownloadIntegration ?? DownloadIntegrationType.Native;

        return integrationType switch
        {
            DownloadIntegrationType.Jellyseerr => _serviceProvider.GetRequiredService<JellyseerrDownloadProvider>(),
            DownloadIntegrationType.Webhook => _serviceProvider.GetRequiredService<WebhookDownloadProvider>(),
            DownloadIntegrationType.Native => _serviceProvider.GetRequiredService<NativeDownloadProvider>(),
            _ => _serviceProvider.GetRequiredService<NativeDownloadProvider>()
        };
    }
}
