using Microsoft.Extensions.Configuration;

namespace TinfoilWebServer.Settings
{
    public interface IAppSettings
    {
        string[] AllowedExt { get; }

        string ServedDirectory { get; }

        IConfiguration KestrelConfig { get; }

        IConfiguration LoggingConfig { get; }

        string? MessageOfTheDay { get; }
    }
}