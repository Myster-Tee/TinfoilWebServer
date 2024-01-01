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
    private readonly CacheSettings _cache = new();
    private readonly FingerprintsFilterSettings _fingerprintsFilter = new();
    private readonly AuthenticationSettings _authentication = new();
    private readonly BlacklistSettings _blacklist = new();
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
        var servedDirectoryPaths = appSettingsModel.ServedDirectories ?? new[] { "./packages" };
        var newServedDirectories = InitializeServedDirectories(servedDirectoryPaths);
        if (!ServedDirectoriesEqual(ServedDirectories, newServedDirectories))
            ServedDirectories = newServedDirectories;

        StripDirectoryNames = appSettingsModel.StripDirectoryNames ?? true;
        ServeEmptyDirectories = appSettingsModel.ServeEmptyDirectories ?? true;

        var allowedExt = appSettingsModel.AllowedExt;
        AllowedExt = allowedExt == null || allowedExt.Length == 0 ? new[] { "xci", "nsz", "nsp" } : allowedExt;

        MessageOfTheDay = string.IsNullOrWhiteSpace(appSettingsModel.MessageOfTheDay) ? null : appSettingsModel.MessageOfTheDay;

        var cache = appSettingsModel.Cache;
        _cache.AutoDetectChanges = cache?.AutoDetectChanges ?? true;
        _cache.PeriodicRefreshDelay = cache?.PeriodicRefreshDelay;

        var authentication = appSettingsModel.Authentication;
        _authentication.Enabled = authentication?.Enabled ?? false;
        _authentication.WebBrowserAuthEnabled = authentication?.WebBrowserAuthEnabled ?? false;
        _authentication.PwdType = authentication?.PwdType ?? PwdType.Plaintext;
        var newUsers = (authentication?.Users ?? Array.Empty<AllowedUserModel>()).OfType<AllowedUserModel>().Select(allowedUserModel =>
            new AllowedUser
            {
                Name = allowedUserModel.Name ?? "",
                MaxFingerprints = allowedUserModel.MaxFingerprints ?? 1,
                Password = allowedUserModel.Pwd ?? "",
                CustomIndexPath = string.IsNullOrWhiteSpace(allowedUserModel.CustomIndexPath) ? null : allowedUserModel.CustomIndexPath,
                MessageOfTheDay = string.IsNullOrWhiteSpace(allowedUserModel.MessageOfTheDay) ? null : allowedUserModel.MessageOfTheDay
            }).ToList();
        if (!UsersEqual(_authentication.Users, newUsers))
            _authentication.Users = newUsers;

        var fingerprintsFilter = appSettingsModel.FingerprintsFilter;
        _fingerprintsFilter.Enabled = fingerprintsFilter?.Enabled ?? false;
        _fingerprintsFilter.FingerprintsFilePath = fingerprintsFilter?.FingerprintsFilePath ?? "AllowedFingerprints.json";
        _fingerprintsFilter.MaxFingerprints = fingerprintsFilter?.MaxFingerprints ?? 0;

        var blacklist = appSettingsModel.Blacklist;
        _blacklist.Enabled = blacklist?.Enabled ?? true;
        _blacklist.FilePath = string.IsNullOrWhiteSpace(blacklist?.FilePath) ? "IpBlacklist.txt" : blacklist.FilePath;
        _blacklist.MaxConsecutiveFailedAuth = blacklist?.MaxConsecutiveFailedAuth ?? 3;
        _blacklist.IsBehindProxy = blacklist?.IsBehindProxy ?? false;

        CustomIndexPath = appSettingsModel.CustomIndexPath;
    }

    private static bool UsersEqual(IReadOnlyList<IAllowedUser> oldUsers, IReadOnlyList<IAllowedUser> newUsers)
    {
        if (oldUsers.Count != newUsers.Count)
            return false;

        return !oldUsers.Where((t, i) => !AllowedUser.Equals(t, newUsers[i])).Any();
    }

    private static bool ServedDirectoriesEqual(IReadOnlyList<DirectoryInfo> oldServedDirectories, IReadOnlyList<DirectoryInfo> newServedDirectories)
    {
        if (oldServedDirectories.Count != newServedDirectories.Count)
            return false;

        return !oldServedDirectories.Where((t, i) => t.FullName != newServedDirectories[i].FullName).Any();
    }

    private List<DirectoryInfo> InitializeServedDirectories(IReadOnlyCollection<string?>? servedDirectoryPaths)
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
                    servedDirectory.Create();
                    _logger.LogInformation($"Served directory \"{servedDirectoryPath}\" created.");
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

    public ICacheSettings Cache => _cache;

    public IFingerprintsFilterSettings FingerprintsFilter => _fingerprintsFilter;

    public IAuthenticationSettings Authentication => _authentication;

    public IBlacklistSettings Blacklist => _blacklist;

    private class CacheSettings : NotifyPropertyChangedBase, ICacheSettings
    {
        private TimeSpan? _periodicRefreshDelay;
        private bool _autoDetectChanges;

        public bool AutoDetectChanges
        {
            get => _autoDetectChanges;
            set => SetField(ref _autoDetectChanges, value);
        }

        public TimeSpan? PeriodicRefreshDelay
        {
            get => _periodicRefreshDelay;
            set => SetField(ref _periodicRefreshDelay, value);
        }
    }

    private class AuthenticationSettings : NotifyPropertyChangedBase, IAuthenticationSettings
    {
        private bool _enabled;
        private IReadOnlyList<IAllowedUser> _users = new List<IAllowedUser>();
        private bool _webBrowserAuthEnabled;
        private PwdType _pwdType;

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

        public PwdType PwdType
        {
            get => _pwdType;
            set => SetField(ref _pwdType, value);
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

        public int MaxFingerprints { get; init; }

        public string Password { get; init; } = "";

        public string? CustomIndexPath { get; init; }

        public string? MessageOfTheDay { get; init; }

        public static bool Equals(IAllowedUser? u1, IAllowedUser? u2)
        {
            if (u1 == null && u2 == null)
                return true;

            if (u1 == null || u2 == null)
                return false;

            return u1.Name == u2.Name
                   && u1.MaxFingerprints == u2.MaxFingerprints
                   && u1.Password == u2.Password
                   && u1.CustomIndexPath == u2.CustomIndexPath
                   && u1.MessageOfTheDay == u2.MessageOfTheDay;
        }

    }

    private class FingerprintsFilterSettings : NotifyPropertyChangedBase, IFingerprintsFilterSettings
    {
        private bool _enabled;
        private string _fingerprintsFilePath = "";
        private int _maxFingerprints;

        public bool Enabled
        {
            get => _enabled;
            set => SetField(ref _enabled, value);
        }

        public string FingerprintsFilePath
        {
            get => _fingerprintsFilePath;
            set => SetField(ref _fingerprintsFilePath, value);
        }

        public int MaxFingerprints
        {
            get => _maxFingerprints;
            set => SetField(ref _maxFingerprints, value);
        }
    }

    private class BlacklistSettings : NotifyPropertyChangedBase, IBlacklistSettings
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

}

