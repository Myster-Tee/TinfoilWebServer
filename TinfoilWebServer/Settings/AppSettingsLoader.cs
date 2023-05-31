using Microsoft.Extensions.Configuration;
using System;

namespace TinfoilWebServer.Settings;

public static class AppSettingsLoader
{

    public static IAppSettings LoadAppSettings(this IConfigurationRoot configRoot)
    {
        var appSettings = new AppSettings();
        configRoot.Bind(appSettings);

        appSettings.KestrelConfig = configRoot.GetSection("Kestrel");
        appSettings.LoggingConfig = configRoot.GetSection("Logging");

        Consolidate(appSettings);

        return appSettings;
    }

    private static void Consolidate(AppSettings appSettings)
    {
        appSettings.AllowedExt ??= new[] { "xci", "nsz", "nsp" };
        appSettings.ServedDirectories ??= new[] { "." };

        Consolidate(appSettings.Authentication);
    }

    private static void Consolidate(AuthenticationSettings? authenticationSettings)
    {
        if (authenticationSettings == null)
            return;

        authenticationSettings.Users ??= Array.Empty<AllowedUser>();
    }
}