using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using TinfoilWebServer.Services;

namespace TinfoilWebServer.Settings
{
    public class AppSettingsLoader
    {

        public static IAppSettings Load(IConfigurationRoot configRoot)
        {
            var appSettings = new AppSettings
            {
                ServedDirectories = GetServedDirectories(configRoot),
                AllowedExt = GetAllowedExt(configRoot),
                MessageOfTheDay = GetMessageOfTheDay(configRoot),
                IndexType = GetIndexType(configRoot),
                KestrelConfig = configRoot.GetSection("Kestrel"),
                LoggingConfig = configRoot.GetSection("Logging")
            };
            return appSettings;
        }

        private static TinfoilIndexType GetIndexType(IConfiguration config)
        {
            var valueStr = config.GetValue<string>("IndexType");

            if (Enum.TryParse(typeof(TinfoilIndexType), valueStr, true, out var value))
                return (TinfoilIndexType)value;
            else
                return TinfoilIndexType.Flatten;
        }

        private static string? GetMessageOfTheDay(IConfiguration config)
        {
            return config.GetValue<string>("MessageOfTheDay");
        }

        private static string[] GetAllowedExt(IConfiguration config)
        {
            var configurationSection = config.GetSection("AllowedExt");
            if (!configurationSection.Exists())
                return new[] { "xci", "nsz", "nsp" };

            var allowedExtensions = configurationSection.GetChildren().Select(section => section.Value).Where(value => value != null).ToArray();
            return allowedExtensions;
        }

        private static string[] GetServedDirectories(IConfiguration config)
        {
            var configurationSection = config.GetSection("ServedDirectories");
            if (!configurationSection.Exists())
                return new[] { Program.CurrentDirectory };

            return configurationSection.GetChildren()
                .Select(section => section.Value)
                .Where(value => value != null).Select(servedDirRaw =>
                {
                    string rootedPath;
                    if (Path.IsPathRooted(servedDirRaw))
                        rootedPath = servedDirRaw;
                    else
                        rootedPath = Path.Combine(Program.CurrentDirectory, servedDirRaw.TrimStart('\\', '/'));

                    var fullPathRooted = Path.GetFullPath(rootedPath);
                    return fullPathRooted;
                })
                .ToArray();
        }
    }
}