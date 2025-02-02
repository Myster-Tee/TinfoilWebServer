using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace TinfoilWebServer.Settings;

public interface IAppSettings : INotifyPropertyChanged
{
    /// <summary>
    /// The served directories
    /// </summary>
    IReadOnlyList<DirectoryInfo> ServedDirectories { get; }

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
    /// The default message of the day
    /// </summary>
    string? MessageOfTheDay { get; }

    /// <summary>
    /// The default message to display when an account is expired
    /// </summary>
    string? ExpirationMessage { get; }

    /// <summary>
    /// The path to a custom JSON index file
    /// </summary>
    string? CustomIndexPath { get; }

    /// <summary>
    /// Cache expiration settings
    /// </summary>
    ICacheSettings Cache { get; }

    /// <summary>
    /// Fingerprints filter settings
    /// </summary>
    IFingerprintsFilterSettings FingerprintsFilter { get; }

    /// <summary>
    /// Authentication settings
    /// </summary>
    IAuthenticationSettings Authentication { get; }

    /// <summary>
    /// Blacklist settings
    /// </summary>
    IBlacklistSettings Blacklist { get; }

}

public interface ICacheSettings : INotifyPropertyChanged
{
    /// <summary>
    /// When true, cache is automatically reloaded when a file system change occurs in served directories
    /// </summary>
    bool AutoDetectChanges { get; }

    /// <summary>
    /// The forced refresh delay
    /// </summary>
    TimeSpan? PeriodicRefreshDelay { get; }
}

public interface IFingerprintsFilterSettings : INotifyPropertyChanged
{
    /// <summary>
    /// Gets a value indicating whether the fingerprints filter is enabled
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    /// The path to the file containing the fingerprints
    /// </summary>
    string FingerprintsFilePath { get; }

    /// <summary>
    /// The maximum number of fingerprints allowed
    /// </summary>
    int MaxFingerprints { get; }
}

public interface IAuthenticationSettings : INotifyPropertyChanged
{
    /// <summary>
    /// True to enable authentication, false otherwise
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    /// When true, a native Web Browser authentication popup is displayed when the user is not authenticated.
    /// Only effective if <see cref="Enabled"/> is true.
    /// </summary>
    bool WebBrowserAuthEnabled { get; }

    /// <summary>
    /// Indicates the password type
    /// </summary>
    PwdType PwdType { get; }

    /// <summary>
    /// The list of allowed users
    /// </summary>
    IReadOnlyList<IAllowedUser> Users { get; }
}

public enum PwdType
{
    Sha256,
    Plaintext
}

public interface IUserInfo
{
    /// <summary>
    /// Name of the user
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The maximum number of fingerprints allowed for this user
    /// </summary>
    int MaxFingerprints { get; init; }

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
    /// Get the account expiration date
    /// </summary>
    DateTime? ExpirationDate { get; }

    /// <summary>
    /// The message to display when the account is expired
    /// </summary>
    string? ExpirationMessage { get; }

    /// <summary>
    /// The password of the allowed user
    /// </summary>
    string Password { get; }

}

public interface IBlacklistSettings : INotifyPropertyChanged
{
    /// <summary>
    /// True if blacklist middleware is enabled, false otherwise
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    /// The file path of blacklisted IP addresses
    /// </summary>
    string FilePath { get; }

    /// <summary>
    /// The maximum number of consecutive failed authentication to reacj
    /// </summary>
    int MaxConsecutiveFailedAuth { get; }

    /// <summary>
    /// When True, indicates that the server is reached from a reverse proxy, in this case the
    /// incoming IP address will be taken from proxy "X-Forwarded-For" header if present.
    /// When False, the incoming ÏP address is taken from TCP/IP protocol.
    /// </summary>
    bool IsBehindProxy { get; }

}