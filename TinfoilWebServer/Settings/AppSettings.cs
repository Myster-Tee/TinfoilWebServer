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
    private string[] _servedDirectories;
    private bool _stripDirectoryNames;
    private bool _serveEmptyDirectories;
    private string[] _allowedExt;
    private string? _messageOfTheDay;
    private string[] _extraRepositories;

    public AppSettings(IOptionsMonitor<AppSettingsModel> appSettingsModel)
    {
        appSettingsModel = appSettingsModel ?? throw new ArgumentNullException(nameof(appSettingsModel));
        InitializeFromModel(appSettingsModel.CurrentValue);

        appSettingsModel.OnChange(InitializeFromModel);
    }

    private void InitializeFromModel(AppSettingsModel appSettingsModel)
    {
        var servedDirectories = appSettingsModel.ServedDirectories;
        ServedDirectories = servedDirectories == null || servedDirectories.Length == 0 ? new[] { "." } : servedDirectories;
        StripDirectoryNames = appSettingsModel.StripDirectoryNames ?? true;
        ServeEmptyDirectories = appSettingsModel.ServeEmptyDirectories ?? true;

        var allowedExt = appSettingsModel.AllowedExt;
        AllowedExt = allowedExt == null || allowedExt.Length == 0 ? new[] { "xci", "nsz", "nsp" } : allowedExt;
        MessageOfTheDay = appSettingsModel.MessageOfTheDay;
        ExtraRepositories = appSettingsModel.ExtraRepositories ?? Array.Empty<string>();

        var cacheExpiration = appSettingsModel.CacheExpiration;
        _cacheExpirationSettings.Enabled = cacheExpiration?.Enabled ?? true;
        _cacheExpirationSettings.ExpirationDelay = cacheExpiration?.ExpirationDelay ?? TimeSpan.FromHours(1);

        var authenticationSettings = appSettingsModel.Authentication;
        _authenticationSettings.Enabled = authenticationSettings?.Enabled ?? false;
        _authenticationSettings.Users = (authenticationSettings?.Users ?? Array.Empty<AllowedUserModel>()).Select(model =>
            new AllowedUser
            {
                Name = model.Name ?? "",
                Password = model.Pwd ?? "",
                MessageOfTheDay = model.MessageOfTheDay,
            }).ToList();
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

    public string[] ExtraRepositories
    {
        get => _extraRepositories;
        private set => SetField(ref _extraRepositories, value);
    }

    public ICacheExpirationSettings CacheExpiration => _cacheExpirationSettings;

    public IAuthenticationSettings Authentication => _authenticationSettings;

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

        public bool Enabled
        {
            get => _enabled;
            set => SetField(ref _enabled, value);
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

        public string? MessageOfTheDay { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is not IAllowedUser other)
                return false;

            return Equals(other);
        }

        public bool Equals(IAllowedUser other)
        {
            return Name == other.Name && Password == other.Password && string.Equals(MessageOfTheDay, other.MessageOfTheDay);
        }

    }

}