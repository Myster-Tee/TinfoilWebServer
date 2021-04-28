using Microsoft.Extensions.Configuration;
using TinfoilWebServer.Services;

namespace TinfoilWebServer.Settings
{
    public interface IAppSettings
    {
        string[] AllowedExt { get; }

        string[] ServedDirectories { get; }

        IConfiguration KestrelConfig { get; }

        IConfiguration LoggingConfig { get; }

        string? MessageOfTheDay { get; }

        TinfoilIndexType IndexType { get; }
    }
}