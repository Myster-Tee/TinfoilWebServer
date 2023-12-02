using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace TinfoilWebServer.Services.FSChangeDetection;

public class DirectoryChangeHelper : IDirectoryChangeHelper
{

    private readonly ILogger<WatchedDirectory> _logger;

    public DirectoryChangeHelper(ILogger<WatchedDirectory> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IWatchedDirectory WatchDirectory(DirectoryInfo directory, bool enableChangeEvent = true)
    {
        return new WatchedDirectory(directory, enableChangeEvent, _logger);
    }
}