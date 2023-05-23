using System;
using Microsoft.Extensions.Configuration;
using TinfoilWebServer.Services;

namespace TinfoilWebServer.Settings;

public class AppSettings : IAppSettings
{
    public string[] AllowedExt { get; init; } = null!;

    public string[] ServedDirectories { get; init; } = null!;

    public IConfiguration KestrelConfig { get; init; } = null!;

    public IConfiguration LoggingConfig { get; init; } = null!;

    public string? MessageOfTheDay { get; init; }

    public TinfoilIndexType IndexType { get; init; }

    public TimeSpan CacheExpiration { get; init; }

    public IAuthenticationSettings? AuthenticationSettings { get; init; }
}