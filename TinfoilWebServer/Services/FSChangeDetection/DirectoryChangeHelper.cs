using Microsoft.Extensions.Logging;
using System;

namespace TinfoilWebServer.Services.FSChangeDetection;

public class DirectoryChangeHelper : IDirectoryChangeHelper
{

    private readonly ILogger<WatchedDirectory> _logger;

    public DirectoryChangeHelper(ILogger<WatchedDirectory> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IWatchedDirectory WatchDirectory(string directoryPath, bool enableChangeEvent = true)
    {
        return new WatchedDirectory(directoryPath, enableChangeEvent, _logger);
    }
}