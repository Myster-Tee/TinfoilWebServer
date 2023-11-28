using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using TinfoilWebServer.Settings.ConfigModels;

namespace TinfoilWebServer.Settings;

public class AppSettings : NotifyPropertyChangedBase, IAppSettings
{
    private readonly CacheExpirationSettings _cacheExpirationSettings = new();
    private readonly AuthenticationSettings _authenticationSettings = new();
    private readonly BlacklistSettings _blacklistSettings = new();
    private string[] _servedDirectories = null!;
    private bool _stripDirectoryNames;
    private bool _serveEmptyDirectories;
    private string[] _allowedExt = null!;
    private string? _messageOfTheDay;
    private string? _customIndexPath;

    public AppSettings(IOptionsMonitor<AppSettingsModel> appSettingsModel)
    {
        appSettingsModel = appSettingsModel ?? throw new ArgumentNullException(nameof(appSettingsModel));
        InitializeFromModel(appSettingsModel.CurrentValue);

        appSettingsModel.OnChange(InitializeFromModel);
    }

    private void InitializeFromModel(AppSettingsModel appSettingsModel)
    {
        var servedDirectories = appSettingsModel.ServedDirectories;
        ServedDirectories = servedDirectories ?? Array.Empty<string>();
        StripDirectoryNames = appSettingsModel.StripDirectoryNames ?? true;
        ServeEmptyDirectories = appSettingsModel.ServeEmptyDirectories ?? true;

        var allowedExt = appSettingsModel.AllowedExt;
        AllowedExt = allowedExt == null || allowedExt.Length == 0 ? new[] { "xci", "nsz", "nsp" } : allowedExt;

        MessageOfTheDay = string.IsNullOrEmpty(appSettingsModel.MessageOfTheDay) ? null : appSettingsModel.MessageOfTheDay;

        var cacheExpiration = appSettingsModel.CacheExpiration;
        _cacheExpirationSettings.Enabled = cacheExpiration?.Enabled ?? true;
        _cacheExpirationSettings.ExpirationDelay = cacheExpiration?.ExpirationDelay ?? TimeSpan.FromHours(1);

        var authenticationSettings = appSettingsModel.Authentication;
        _authenticationSettings.Enabled = authenticationSettings?.Enabled ?? false;
        _authenticationSettings.WebBrowserAuthEnabled = authenticationSettings?.WebBrowserAuthEnabled ?? false;
        _authenticationSettings.Users = (authenticationSettings?.Users ?? Array.Empty<AllowedUserModel>()).Select(model =>
            new AllowedUser
            {
                Name = model.Name ?? "",
                Password = model.Pwd ?? "",
                CustomIndexPath = model.CustomIndexPath,
            }).ToList();

        var blacklistSettings = appSettingsModel.Blacklist;
        _blacklistSettings.Enabled = blacklistSettings?.Enabled ?? true;
        _blacklistSettings.FilePath = blacklistSettings?.FilePath ?? "IpBlacklist.txt";
        _blacklistSettings.MaxConsecutiveFailedAuth = blacklistSettings?.MaxConsecutiveFailedAuth ?? 3;
        _blacklistSettings.IsBehindProxy = blacklistSettings?.IsBehindProxy ?? false;

        CustomIndexPath = appSettingsModel.CustomIndexPath;
    }

    public string[] ServedDirectories
    {
        get => _servedDirectories;
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

    public string[] AllowedExt
    {
        get => _allowedExt;
        private set => SetField(ref _allowedExt, value);
    }

    public string? MessageOfTheDay
    {
        get => _messageOfTheDay;
        private set => SetField(ref _messageOfTheDay, value);
    }

    public ICacheExpirationSettings CacheExpiration => _cacheExpirationSettings;

    public IAuthenticationSettings Authentication => _authenticationSettings;

    public IBlacklistSettings BlacklistSettings => _blacklistSettings;

    public string? CustomIndexPath
    {
        get => _customIndexPath;
        private set => SetField(ref _customIndexPath, value);
    }

    private class CacheExpirationSettings : NotifyPropertyChangedBase, ICacheExpirationSettings
    {
        private bool _enabled;
        private TimeSpan _expirationDelay;

        public bool Enabled
        {
            get => _enabled;
            set => SetField(ref _enabled, value);
        }

        public TimeSpan ExpirationDelay
        {
            get => _expirationDelay;
            set => SetField(ref _expirationDelay, value);
        }
    }

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
        public string Name { get; set; } = "";

        public string Password { get; set; } = "";

        public string? CustomIndexPath { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is not IAllowedUser other)
                return false;

            return Equals(other);
        }

        public bool Equals(IAllowedUser other)
        {
            return Name == other.Name && Password == other.Password && string.Equals(CustomIndexPath, other.CustomIndexPath);
        }

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