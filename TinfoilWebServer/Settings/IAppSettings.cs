using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace TinfoilWebServer.Settings;

public interface IAppSettings
{
    /// <summary>
    /// The list of served directories
    /// </summary>
    string[] ServedDirectories { get; }

    /// <summary>
    /// Removes directories names in URLs of served files
    /// </summary>
    bool StripDirectoryNames { get; }

    /// <summary>
    /// True to serve empty directories
    /// </summary>
    bool ServeEmptyDirectories { get; }

    /// <summary>
    /// The list of allowed extensions
    /// </summary>
    string[] AllowedExt { get; }

    /// <summary>
    /// The message displayed by Tinfoil at startup
    /// </summary>
    string? MessageOfTheDay { get; }

    /// <summary>
    /// A set of extra repositories sent to Tinfoil for scanning
    /// </summary>
    string[] ExtraRepositories { get; }

    /// <summary>
    /// The web server configuration
    /// </summary>
    IConfiguration? KestrelConfig { get; }

    /// <summary>
    /// The logging configuration
    /// </summary>
    IConfiguration? LoggingConfig { get; }

    /// <summary>
    /// Cache expiration settings
    /// </summary>
    ICacheExpirationSettings? CacheExpiration { get; }

    /// <summary>
    /// Authentication settings
    /// </summary>
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

    public IReadOnlyList<IAllowedUser> Users { get; }
}

public interface IAllowedUser
{
    public string Name { get; }

    public string Password { get; }
}

