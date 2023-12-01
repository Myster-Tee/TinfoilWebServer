using System.Collections.Generic;
using System.ComponentModel;

namespace TinfoilWebServer.Settings;

public interface IAppSettings : INotifyPropertyChanged
{
    /// <summary>
    /// The served directories
    /// </summary>
    IReadOnlyList<string> ServedDirectories { get; }

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
    IReadOnlyList<string> AllowedExt { get; }

    /// <summary>
    /// The message of the day
    /// </summary>
    string? MessageOfTheDay { get; }

    /// <summary>
    /// The path to a custom JSON index file
    /// </summary>
    string? CustomIndexPath { get; }

    /// <summary>
    /// Authentication settings
    /// </summary>
    IAuthenticationSettings Authentication { get; }

    /// <summary>
    /// Blacklist settings
    /// </summary>
    IBlacklistSettings BlacklistSettings { get; }

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

public interface IUserInfo
{
    /// <summary>
    /// Name of the user
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The path to a custom JSON index file
    /// </summary>
    string? CustomIndexPath { get; }

    /// <summary>
    /// Message of the day for the user
    /// </summary>
    string? MessageOfTheDay { get; }

}

public interface IAllowedUser : IUserInfo
{

    /// <summary>
    /// The password of the allowed user
    /// </summary>
    public string Password { get; }

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