using System;

namespace TinfoilWebServer.Settings.ConfigModels;

/// <summary>
/// Model of the JSON settings automatically deserialized by ASP.NET configuration system
/// </summary>
public class AppSettingsModel
{
    public string[]? ServedDirectories { get; set; }

    public bool? StripDirectoryNames { get; set; }

    public bool? ServeEmptyDirectories { get; set; } = false;

    public string[]? AllowedExt { get; set; }

    public string? MessageOfTheDay { get; set; }

    public string? CustomIndexPath { get; set; }

    public CacheSettingsModel? Cache { get; set; }

    public AuthenticationSettingsModel? Authentication { get; set; }

    public BlacklistSettingsModel? Blacklist { get; set; }

}

public class CacheSettingsModel
{
    public bool? AutoDetectChanges { get; set; }

    public TimeSpan? ForcedRefreshDelay { get; set; }
}

public class AuthenticationSettingsModel
{

    public bool? Enabled { get; set; } = true;

    public bool WebBrowserAuthEnabled { get; set; } = false;

    public AllowedUserModel[]? Users { get; set; }
}

public class AllowedUserModel
{
    public string? Name { get; set; }

    public string? Pwd { get; set; }

    public string? CustomIndexPath { get; set; }

    public string? MessageOfTheDay { get; set; }
}

public class BlacklistSettingsModel
{
    public bool? Enabled { get; set; }

    public string? FilePath { get; set; }

    public int? MaxConsecutiveFailedAuth { get; set; }

    public bool? IsBehindProxy { get; set; }
}