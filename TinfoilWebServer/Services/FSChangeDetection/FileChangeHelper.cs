using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace TinfoilWebServer.Services.FSChangeDetection;

public class FileChangeHelper : IFileChangeHelper
{

    private readonly ILogger<WatchedFile> _logger;

    public FileChangeHelper(ILogger<WatchedFile> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IWatchedFile WatchFile(FileInfo file, bool enableChangeEvent = true)
    {
        return new WatchedFile(file, enableChangeEvent, _logger);
    }

}