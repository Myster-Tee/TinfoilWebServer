using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TinfoilWebServer.Settings;

public interface IAppSettings : INotifyPropertyChanged
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
    /// Cache expiration settings
    /// </summary>
    ICacheExpirationSettings CacheExpiration { get; }

    /// <summary>
    /// Authentication settings
    /// </summary>
    IAuthenticationSettings Authentication { get; }

    /// <summary>
    /// Blacklist settings
    /// </summary>
    IBlacklistSettings BlacklistSettings { get; }

    /// <summary>
    /// The path to a custom JSON index file
    /// </summary>
    string? CustomIndexPath { get; }

}

public interface ICacheExpirationSettings : INotifyPropertyChanged
{
    bool Enabled { get; }

    TimeSpan ExpirationDelay { get; }
}

public interface IAuthenticationSettings : INotifyPropertyChanged
{
    /// <summary>
    /// True to enable authentication, false otherwise
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// When true, a native web browser authentication popup is displayed when the user is not authenticated.
    /// Only effective if <see cref="Enabled"/> is true.
    /// </summary>
    public bool WebBrowserAuthEnabled { get; }

    /// <summary>
    /// The list of allowed users
    /// </summary>
    public IReadOnlyList<IAllowedUser> Users { get; }
}

public interface IAllowedUser
{
    /// <summary>
    /// Name of the user
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The password of the allowed user
    /// </summary>
    public string Password { get; }

    /// <summary>
    /// A message of the day specific to this user
    /// </summary>
    public string? MessageOfTheDay { get; }

}

public interface IBlacklistSettings : INotifyPropertyChanged
{
    /// <summary>
    /// True if blacklist middleware is enabled, false otherwise
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// The file path of blacklisted IP addresses
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// The maximum number of consecutive failed authentication to reacj
    /// </summary>
    public int MaxConsecutiveFailedAuth { get; }

    /// <summary>
    /// When True, indicates that the server is reached from a reverse proxy, in this case the
    /// incoming IP address will be taken from proxy "X-Forwarded-For" header if present.
    /// When False, the incoming ÏP address is taken from TCP/IP protocol.
    /// </summary>
    public bool IsBehindProxy { get; }

}