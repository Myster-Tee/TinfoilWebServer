using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using TinfoilWebServer.Services;

namespace TinfoilWebServer.Settings;

public interface IAppSettings
{
    string[] AllowedExt { get; }

    /// <summary>
    /// The list of served directories
    /// </summary>
    string[] ServedDirectories { get; }

    IConfiguration? KestrelConfig { get; }

    IConfiguration? LoggingConfig { get; }

    string? MessageOfTheDay { get; }

    TinfoilIndexType IndexType { get; }

    ICacheExpirationSettings? CacheExpiration { get; }

    IAuthenticationSettings? Authentication { get; }
}

public interface ICacheExpirationSettings
{
    bool Enabled { get; }

    TimeSpan ExpirationDelay { get; }
}

public interface IAuthenticationSettings
{
    public bool Enabled { get; }

    public IReadOnlyList<IAllowedUser> AllowedUsers { get; }
}

public interface IAllowedUser
{
    public string Name { get; }

    public string Password { get; }
}

