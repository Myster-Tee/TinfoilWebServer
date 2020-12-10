using Microsoft.Extensions.Configuration;

namespace TinfoilWebServer.Settings
{
    public class AppSettings : IAppSettings
    {
        public string[] AllowedExt { get; set; }

        public string ServedDirectory { get; set; }

        public IConfiguration KestrelConfig { get; set; }

        public IConfiguration LoggingConfig { get; set; }

        public string? MessageOfTheDay { get; set; }

        public TinfoilIndexType IndexType { get; set; }
    }
}
