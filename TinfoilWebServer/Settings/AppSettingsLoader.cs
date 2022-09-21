using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using TinfoilWebServer.Services;

namespace TinfoilWebServer.Settings;

public class AppSettingsLoader
{

    public static IAppSettings Load(IConfigurationRoot configRoot, out string[] loadingErrors)
    {
        var loader = new AppSettingsLoaderInternal(configRoot);
        return loader.Load(out loadingErrors);
    }


    private class AppSettingsLoaderInternal
    {
        private readonly IConfigurationRoot _configurationRoot;
        private readonly List<string> _loadingErrors = new();


        public AppSettingsLoaderInternal(IConfigurationRoot configurationRoot)
        {
            _configurationRoot = configurationRoot;
        }

        public IAppSettings Load(out string[] loadingErrors)
        {
            _loadingErrors.Clear();

            var appSettings = new AppSettings
            {
                ServedDirectories = GetServedDirectories(),
                AllowedExt = GetAllowedExt(),
                MessageOfTheDay = GetMessageOfTheDay(),
                IndexType = GetIndexType(),
                CacheExpiration = GetCacheExpiration(),
                KestrelConfig = _configurationRoot.GetSection("Kestrel"),
                LoggingConfig = _configurationRoot.GetSection("Logging"),
                AuthenticationSettings = GetAuthentication(),
            };

            loadingErrors = _loadingErrors.ToArray();

            return appSettings;
        }

        private IAuthenticationSettings? GetAuthentication()
        {
            const string? SECTION_NAME = "Authentication";

            var authenticationSection = _configurationRoot.GetSection(SECTION_NAME);
            if (authenticationSection == null)
                return null;


            var authentication = new AuthenticationSettings
            {
                Enabled = authenticationSection.GetValue("Enabled", true),
                AllowedUsers = LoadUsers(authenticationSection).ToArray()
            };

            return authentication;
        }

        private IEnumerable<IAllowedUser> LoadUsers(IConfiguration authenticationSection)
        {
            var configurationSection = authenticationSection.GetSection("Users");

            foreach (var user in configurationSection.GetChildren())
            {
                var name = user.GetValue<string>("Name");
                if (string.IsNullOrWhiteSpace(name))
                    _loadingErrors.Add($"Empty user name found.");

                var pwd = user.GetValue<string>("Pwd");
                if (string.IsNullOrWhiteSpace(pwd))
                    _loadingErrors.Add($"Empty password found for user «{name}».");

                yield return new AllowedUser
                {
                    Name = name,
                    Password = pwd
                };
            }
        }

        private TimeSpan GetCacheExpiration()
        {
            const string? SETTING_NAME = "CacheExpiration";

            var cacheExpirationRaw = _configurationRoot.GetValue<string>(SETTING_NAME);
            if (cacheExpirationRaw == null)
                return TimeSpan.Zero;

            if (!TimeSpan.TryParseExact(cacheExpirationRaw, "c", null, TimeSpanStyles.None, out var cacheExpiration)) // Format for "c": [d'.']hh':'mm':'ss['.'fffffff]
            {
                _loadingErrors.Add($"Invalid setting «{SETTING_NAME}», expected format «[d'.']hh':'mm':'ss['.'fffffff]».");
                return TimeSpan.Zero;
            }

            if (cacheExpiration.Ticks < 0)
            {
                _loadingErrors.Add($"Invalid setting «{SETTING_NAME}», value can't be negative.");
                return TimeSpan.Zero;
            }

            return cacheExpiration;
        }

        private TinfoilIndexType GetIndexType()
        {
            const string? SETTING_NAME = "IndexType";

            var valueStr = _configurationRoot.GetValue<string>(SETTING_NAME);

            if (!Enum.TryParse<TinfoilIndexType>(valueStr, true, out var value))
            {
                var allowedValues = string.Join(", ", Enum.GetValues<TinfoilIndexType>().Select(v => v.ToString()));
                _loadingErrors.Add($"Invalid setting «{SETTING_NAME}», allowed values «{allowedValues}».");
                return TinfoilIndexType.Flatten;
            }

            return value;
        }

        private string? GetMessageOfTheDay()
        {
            return _configurationRoot.GetValue<string?>("MessageOfTheDay");
        }

        private string[] GetAllowedExt()
        {
            var configurationSection = _configurationRoot.GetSection("AllowedExt");
            if (!configurationSection.Exists())
                return new[] { "xci", "nsz", "nsp" };

            var allowedExtensions = configurationSection.GetChildren().Select(section => section.Value).Where(value => value != null).ToArray();
            return allowedExtensions;
        }

        private string[] GetServedDirectories()
        {
            var configurationSection = _configurationRoot.GetSection("ServedDirectories");
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