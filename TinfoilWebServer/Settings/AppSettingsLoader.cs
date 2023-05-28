using Microsoft.Extensions.Configuration;
using System;

namespace TinfoilWebServer.Settings;

public static class AppSettingsLoader
{

    public static IAppSettings LoadAppSettings(this IConfigurationRoot configRoot)
    {
        var appSettings = configRoot.Get<AppSettings>();

        CheckConsistency(appSettings);

        appSettings.KestrelConfig = configRoot.GetSection("Kestrel");
        appSettings.LoggingConfig = configRoot.GetSection("Logging");

        return appSettings;
    }

    private static void CheckConsistency(IAppSettings? appSettings)
    {
        if (appSettings == null)
            throw new InvalidSettingException("Settings are missing!");

        if (appSettings.AllowedExt == null)
            throw new InvalidSettingException($"Setting \"{nameof(IAppSettings.AllowedExt)}\" is missing!");

        if (appSettings.ServedDirectories == null)
            throw new InvalidSettingException($"Setting \"{nameof(IAppSettings.ServedDirectories)}\" is missing!");

        CheckConsistency(appSettings.Authentication);
    }

    private static void CheckConsistency(IAuthenticationSettings? authenticationSettings)
    {
        if (authenticationSettings == null)
            return;

        if (authenticationSettings.AllowedUsers == null)
            throw new InvalidSettingException($"Setting \"{nameof(IAuthenticationSettings.AllowedUsers)}\" is missing!");
    }
}


public class InvalidSettingException : Exception
{
    public InvalidSettingException(string message) : base(message)
    {

    }
}