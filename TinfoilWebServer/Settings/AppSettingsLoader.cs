using Microsoft.Extensions.Configuration;

namespace TinfoilWebServer.Settings;

public static class AppSettingsLoader
{

    public static IAppSettings LoadAppSettings(this IConfigurationRoot configRoot)
    {
        var appSettings = new AppSettings();
        configRoot.Bind(appSettings);

        appSettings.KestrelConfig = configRoot.GetSection("Kestrel");
        appSettings.LoggingConfig = configRoot.GetSection("Logging");

        return appSettings;
    }

}