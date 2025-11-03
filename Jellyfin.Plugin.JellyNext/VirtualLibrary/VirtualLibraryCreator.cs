using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyNext.VirtualLibrary;

/// <summary>
/// Initializes the JellyNext virtual library on plugin startup.
/// </summary>
public class VirtualLibraryCreator
{
    private readonly VirtualLibraryManager _libraryManager;
    private readonly ILogger<VirtualLibraryCreator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualLibraryCreator"/> class.
    /// </summary>
    /// <param name="libraryManager">The virtual library manager.</param>
    /// <param name="logger">The logger.</param>
    public VirtualLibraryCreator(VirtualLibraryManager libraryManager, ILogger<VirtualLibraryCreator> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the virtual library.
    /// </summary>
    public void Initialize()
    {
        _logger.LogInformation("Initializing JellyNext virtual library");
        _libraryManager.Initialize();
    }
}
