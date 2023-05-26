using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using TinfoilWebServer.Services;

namespace TinfoilWebServer.Settings;

public class AppSettings : IAppSettings
{
    [Required]
    public string[] AllowedExt { get; set; }

    [Required]
    public string[] ServedDirectories { get; set; }

    public IConfiguration? KestrelConfig { get; set; }

    public IConfiguration? LoggingConfig { get; set; }

    public string? MessageOfTheDay { get; set; }

    public TinfoilIndexType IndexType { get; set; }

    public TimeSpan CacheExpiration { get; set; }

    public AuthenticationSettings? Authentication { get; set; }

    IAuthenticationSettings? IAppSettings.Authentication => Authentication;
}