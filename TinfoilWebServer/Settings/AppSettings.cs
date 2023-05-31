using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using TinfoilWebServer.Services;

namespace TinfoilWebServer.Settings;

public class AppSettings : IAppSettings
{
    public string[]? AllowedExt { get; set; }

    public string[]? ServedDirectories { get; set; }

    public IConfiguration? KestrelConfig { get; set; }

    public IConfiguration? LoggingConfig { get; set; }

    public string? MessageOfTheDay { get; set; }

    public TinfoilIndexType IndexType { get; set; } = TinfoilIndexType.Flatten;

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

    public bool Enabled { get; set; } = true!;

    public AllowedUser[]? Users { get; set; }

    IReadOnlyList<IAllowedUser> IAuthenticationSettings.AllowedUsers => Users ?? Array.Empty<IAllowedUser>();

}

public class AllowedUser : IAllowedUser
{
    public string Name { get; set; } = null!;


    public string Password { get; set; } = null!;
}