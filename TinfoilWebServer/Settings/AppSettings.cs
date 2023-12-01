using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TinfoilWebServer.Booting;
using TinfoilWebServer.Settings.ConfigModels;

namespace TinfoilWebServer.Settings;

public class AppSettings : NotifyPropertyChangedBase, IAppSettings
{
    private readonly ILogger<AppSettings> _logger;
    private readonly IBootInfo _bootInfo;
    private readonly CacheSettings _cacheSettings = new();
    private readonly AuthenticationSettings _authenticationSettings = new();
    private readonly BlacklistSettings _blacklistSettings = new();
    private IReadOnlyList<DirectoryInfo> _servedDirectories = Array.Empty<DirectoryInfo>();
    private bool _stripDirectoryNames;
    private bool _serveEmptyDirectories;
    private IReadOnlyList<string> _allowedExt = null!;
    private string? _messageOfTheDay;
    private string? _customIndexPath;

    public AppSettings(IOptionsMonitor<AppSettingsModel> appSettingsModel, ILogger<AppSettings> logger, IBootInfo bootInfo)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _bootInfo = bootInfo ?? throw new ArgumentNullException(nameof(bootInfo));
        appSettingsModel = appSettingsModel ?? throw new ArgumentNullException(nameof(appSettingsModel));
        InitializeFromModel(appSettingsModel.CurrentValue);

        appSettingsModel.OnChange(InitializeFromModel);
    }

    /// <summary>
    /// Called when settings file is updated
    /// </summary>
    /// <param name="appSettingsModel"></param>
    private void InitializeFromModel(AppSettingsModel appSettingsModel)
    {
        var servedDirectories = appSettingsModel.ServedDirectories;
        ServedDirectories = InitializeServedDirectories(servedDirectories);
        StripDirectoryNames = appSettingsModel.StripDirectoryNames ?? true;
        ServeEmptyDirectories = appSettingsModel.ServeEmptyDirectories ?? true;

        var allowedExt = appSettingsModel.AllowedExt;
        AllowedExt = allowedExt == null || allowedExt.Length == 0 ? new[] { "xci", "nsz", "nsp" } : allowedExt;

        MessageOfTheDay = string.IsNullOrWhiteSpace(appSettingsModel.MessageOfTheDay) ? null : appSettingsModel.MessageOfTheDay;

        var cacheExpiration = appSettingsModel.Cache;
        _cacheSettings.AutoDetectChanges = cacheExpiration?.AutoDetectChanges ?? true;
        _cacheSettings.ForcedRefreshDelay = cacheExpiration?.ForcedRefreshDelay;

        var authenticationSettings = appSettingsModel.Authentication;
        _authenticationSettings.Enabled = authenticationSettings?.Enabled ?? false;
        _authenticationSettings.WebBrowserAuthEnabled = authenticationSettings?.WebBrowserAuthEnabled ?? false;
        _authenticationSettings.Users = (authenticationSettings?.Users ?? Array.Empty<AllowedUserModel>()).Select(allowedUserModel =>
            new AllowedUser
            {
                Name = allowedUserModel.Name ?? "",
                Password = allowedUserModel.Pwd ?? "",
                CustomIndexPath = string.IsNullOrWhiteSpace(allowedUserModel.CustomIndexPath) ? null : allowedUserModel.CustomIndexPath,
                MessageOfTheDay = string.IsNullOrWhiteSpace(allowedUserModel.MessageOfTheDay) ? null : allowedUserModel.MessageOfTheDay
            }).ToList();

        var blacklistSettings = appSettingsModel.Blacklist;
        _blacklistSettings.Enabled = blacklistSettings?.Enabled ?? true;
        _blacklistSettings.FilePath = string.IsNullOrWhiteSpace(blacklistSettings?.FilePath) ? "IpBlacklist.txt" : blacklistSettings.FilePath;
        _blacklistSettings.MaxConsecutiveFailedAuth = blacklistSettings?.MaxConsecutiveFailedAuth ?? 3;
        _blacklistSettings.IsBehindProxy = blacklistSettings?.IsBehindProxy ?? false;

        CustomIndexPath = appSettingsModel.CustomIndexPath;
    }

    private IReadOnlyList<DirectoryInfo> InitializeServedDirectories(IReadOnlyCollection<string?>? servedDirectoryPaths)
    {
        var servedDirectories = new List<DirectoryInfo>();

        if (servedDirectoryPaths == null || servedDirectoryPaths.Count <= 0)
        {
            _logger.LogWarning($"No served directory defined in configuration file \"{_bootInfo.ConfigFileFullPath}\".");
            return servedDirectories;
        }

        foreach (var servedDirectoryPath in servedDirectoryPaths)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(servedDirectoryPath))
                {
                    _logger.LogError("Invalid configuration, served directory path can't be empty.");
                    continue;
                }

                var servedDirectory = new DirectoryInfo(servedDirectoryPath);
                if (!servedDirectory.Exists)
                {
                    _logger.LogError($"Served directory \"{servedDirectoryPath}\" doesn't exist.");
                    continue;
                }

                servedDirectories.Add(servedDirectory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while initializing served directory \"{servedDirectoryPath}\": {ex.Message}");
            }
        }

        if (servedDirectories.Count <= 0)
            _logger.LogWarning($"No valid served directory found in configuration file \"{_bootInfo.ConfigFileFullPath}\".");

        return servedDirectories;
    }

    public IReadOnlyList<DirectoryInfo> ServedDirectories
    {
        get => _servedDirectories.ToArray();
        private set => SetField(ref _servedDirectories, value);
    }

    public bool StripDirectoryNames
    {
        get => _stripDirectoryNames;
        private set => SetField(ref _stripDirectoryNames, value);
    }

    public bool ServeEmptyDirectories
    {
        get => _serveEmptyDirectories;
        private set => SetField(ref _serveEmptyDirectories, value);
    }

    public IReadOnlyList<string> AllowedExt
    {
        get => _allowedExt;
        private set => SetField(ref _allowedExt, value);
    }

    public string? MessageOfTheDay
    {
        get => _messageOfTheDay;
        private set => SetField(ref _messageOfTheDay, value);
    }

    public string? CustomIndexPath
    {
        get => _customIndexPath;
        private set => SetField(ref _customIndexPath, value);
    }

    public ICacheSettings Cache => _cacheSettings;

    public IAuthenticationSettings Authentication => _authenticationSettings;

    public IBlacklistSettings Blacklist => _blacklistSettings;

    private class AuthenticationSettings : NotifyPropertyChangedBase, IAuthenticationSettings
    {
        private bool _enabled;
        private IReadOnlyList<IAllowedUser> _users = new List<IAllowedUser>();
        private bool _webBrowserAuthEnabled;

        public bool Enabled
        {
            get => _enabled;
            set => SetField(ref _enabled, value);
        }

        public bool WebBrowserAuthEnabled
        {
            get => _webBrowserAuthEnabled;
            set => SetField(ref _webBrowserAuthEnabled, value);
        }

        public IReadOnlyList<IAllowedUser> Users
        {
            get => _users;
            set => SetField(ref _users, value);
        }
    }

    private class AllowedUser : IAllowedUser
    {
        public string Name { get; init; } = "";

        public string Password { get; init; } = "";

        public string? CustomIndexPath { get; init; }

        public string? MessageOfTheDay { get; init; }
    }

}

internal class CacheSettings : NotifyPropertyChangedBase, ICacheSettings
{
    private TimeSpan? _forcedRefreshDelay;
    private bool _autoDetectChanges;

    public bool AutoDetectChanges
    {
        get => _autoDetectChanges;
        set => SetField(ref _autoDetectChanges, value);
    }

    public TimeSpan? ForcedRefreshDelay
    {
        get => _forcedRefreshDelay;
        set => SetField(ref _forcedRefreshDelay, value);
    }
}

public class BlacklistSettings : NotifyPropertyChangedBase, IBlacklistSettings
{
    private bool _enabled;
    private string _filePath = "";
    private int _maxConsecutiveFailedAuth;
    private bool _isBehindProxy;

    public bool Enabled
    {
        get => _enabled;
        set => SetField(ref _enabled, value);
    }

    public string FilePath
    {
        get => _filePath;
        set => SetField(ref _filePath, value);
    }

    public int MaxConsecutiveFailedAuth
    {
        get => _maxConsecutiveFailedAuth;
        set => SetField(ref _maxConsecutiveFailedAuth, value);
    }

    public bool IsBehindProxy
    {
        get => _isBehindProxy;
        set => SetField(ref _isBehindProxy, value);
    }
}