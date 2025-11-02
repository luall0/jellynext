using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyNext.Models;

namespace Jellyfin.Plugin.JellyNext.Providers;

/// <summary>
/// Interface for content providers that fetch media from external sources.
/// </summary>
public interface IContentProvider
{
    /// <summary>
    /// Gets the unique name of this content provider.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets the display name of the virtual library for this provider.
    /// </summary>
    string LibraryName { get; }

    /// <summary>
    /// Fetches content for a specific user.
    /// </summary>
    /// <param name="userId">The Jellyfin user ID.</param>
    /// <returns>A collection of content items.</returns>
    Task<IReadOnlyList<ContentItem>> FetchContentAsync(Guid userId);

    /// <summary>
    /// Determines if this provider is enabled for the given user.
    /// </summary>
    /// <param name="userId">The Jellyfin user ID.</param>
    /// <returns>True if enabled, false otherwise.</returns>
    bool IsEnabledForUser(Guid userId);
}
