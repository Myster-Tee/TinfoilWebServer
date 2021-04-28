using System;
using System.Collections.Generic;
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
                ServedDirectories = GetServedDirectories(configRoot).ToArray(),
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

        private static IEnumerable<string> GetServedDirectories(IConfiguration config)
        {
            var values = config.GetValue<string[]?>("ServedDirectory");
            if (values != null)
            {
                foreach (var value in values)
                {
                    string rootedPath;
                    if (Path.IsPathRooted(value))
                        rootedPath = value;
                    else
                        rootedPath = Path.Combine(Program.CurrentDirectory, value.TrimStart('\\', '/'));

                    yield return rootedPath;
                }
                yield break;
            }

            yield return Program.CurrentDirectory;
        }
    }
}