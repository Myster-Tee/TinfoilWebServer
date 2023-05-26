using Microsoft.Extensions.Configuration;

namespace TinfoilWebServer.Settings;

public static class AppSettingsLoader
{

    public static IAppSettings Load(this IConfigurationRoot configRoot)
    {
        var appSettings = configRoot.Get<AppSettings>();
        appSettings.KestrelConfig = configRoot.GetSection("Kestrel");
        appSettings.LoggingConfig = configRoot.GetSection("Logging");
        return appSettings;
    }

}