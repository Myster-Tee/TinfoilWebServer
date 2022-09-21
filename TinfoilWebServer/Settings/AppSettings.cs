using System;
using Microsoft.Extensions.Configuration;
using TinfoilWebServer.Services;

namespace TinfoilWebServer.Settings;

public class AppSettings : IAppSettings
{
    public string[] AllowedExt { get; init; }

    public string[] ServedDirectories { get; init; }

    public IConfiguration KestrelConfig { get; init; }

    public IConfiguration LoggingConfig { get; init; }

    public string? MessageOfTheDay { get; init; }

    public TinfoilIndexType IndexType { get; init; }

    public TimeSpan CacheExpiration { get; init; }

    public IAuthenticationSettings? AuthenticationSettings { get; init; }
}