using System;
using System.IO;
using System.Linq;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services;

public class FileFilter : IFileFilter
{
    private readonly IAppSettings _appSettings;

    public FileFilter(IAppSettings appSettings)
    {
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
    }

    public bool IsFileAllowed(string? filePath)
    {
        if (filePath == null)
            return false;

        var currentExtension = Path.GetExtension(filePath).TrimStart('.');
        var ext = _appSettings.AllowedExt.FirstOrDefault(allowedExtension => string.Equals(allowedExtension, currentExtension, StringComparison.OrdinalIgnoreCase));
        return ext != null;
    }
}