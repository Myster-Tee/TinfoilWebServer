using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace TinfoilWebServer.Settings;

public class AppSettings : IAppSettings
{
    public string[]? ServedDirectories { get; set; }

    string[] IAppSettings.ServedDirectories
    {
        get { return this.ServedDirectories ??= new[] { "." }; }
    }

    public string[]? AllowedExt { get; set; }

    string[] IAppSettings.AllowedExt
    {
        get { return this.AllowedExt ??= new[] { "xci", "nsz", "nsp" }; }
    }

    public string? MessageOfTheDay { get; set; }

    public string[]? ExtraRepositories { get; set; }

    string[] IAppSettings.ExtraRepositories
    {
        get { return this.ExtraRepositories ??= Array.Empty<string>(); }
    }

    public IConfiguration? KestrelConfig { get; set; }

    public IConfiguration? LoggingConfig { get; set; }

    public CacheExpirationSettings? CacheExpiration { get; set; }

    ICacheExpirationSettings? IAppSettings.CacheExpiration => CacheExpiration;

    public AuthenticationSettings? Authentication { get; set; }

    IAuthenticationSettings? IAppSettings.Authentication => Authentication;

}

public class CacheExpirationSettings : ICacheExpirationSettings
{
    public bool Enabled { get; set; }

    public TimeSpan ExpirationDelay { get; set; }
}

public class AuthenticationSettings : IAuthenticationSettings
{

    public bool Enabled { get; set; } = true;

    public AllowedUser[]? Users { get; set; }

    IReadOnlyList<IAllowedUser> IAuthenticationSettings.Users => Users ??= Array.Empty<AllowedUser>();

}

public class AllowedUser : IAllowedUser
{
    public string? Name { get; set; }

    string IAllowedUser.Name => this.Name ??= "";

    public string? Password { get; set; }

    string IAllowedUser.Password => this.Password ??= "";
}