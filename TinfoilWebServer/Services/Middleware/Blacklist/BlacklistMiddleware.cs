using System;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services.Middleware.Blacklist;

public class BlacklistMiddleware : IBlacklistMiddleware
{
    private readonly IBlacklistManager _blacklistManager;
    private readonly ILogger<BlacklistMiddleware> _logger;
    private readonly IBlacklistSettings _blacklistSettings;

    public BlacklistMiddleware(IBlacklistManager blacklistManager, IBlacklistSettings blacklistSettings, ILogger<BlacklistMiddleware> logger)
    {
        _blacklistManager = blacklistManager ?? throw new ArgumentNullException(nameof(blacklistManager));
        _blacklistSettings = blacklistSettings ?? throw new ArgumentNullException(nameof(blacklistSettings));
        _blacklistSettings.PropertyChanged += OnBlacklistSettingsChanged;

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private void OnBlacklistSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IBlacklistSettings.IsBehindProxy))
        {
            if (_blacklistSettings.IsBehindProxy)
                _logger.LogInformation("Server marked as being behind proxy.");
            else
                _logger.LogInformation("Server not marked as being behind proxy.");
        }
    }

    /// <summary>
    /// Incoming request processing
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var remoteIpAddress = GetRemoteIpAddress(context);
        if (remoteIpAddress == null)
        {
            _logger.LogWarning($"Request [{context.TraceIdentifier}] rejected, no remote IP address found.");
            await RespondUnauthorized(context);
            return;
        }

        if (_blacklistManager.IsIpBlacklisted(remoteIpAddress))
        {
            _logger.LogInformation($"Request [{context.TraceIdentifier}] rejected, IP address \"{remoteIpAddress}\" blacklisted.");
            await RespondUnauthorized(context);
            return;
        }

        _logger.LogDebug($"Request [{context.TraceIdentifier}] from IP Address \"{remoteIpAddress}\".");

        await next.Invoke(context);

        if (context.IsBlacklistingDisabled())
        {
            // Here, blacklisting has been disabled by another middleware, in this case it is better not to report the IP address as authorized
            return;
        }

        if (context.Response.StatusCode is >= StatusCodes.Status401Unauthorized and <= StatusCodes.Status403Forbidden)
        {
            _blacklistManager.ReportIpUnauthorized(remoteIpAddress);
        }
        else
        {
            _blacklistManager.ReportIpAuthorized(remoteIpAddress);
        }

    }

    private IPAddress? GetRemoteIpAddress(HttpContext context)
    {
        if (_blacklistSettings.IsBehindProxy)
        {
            const string EXPECTED_HEADER = "X-Forwarded-For";
            if (context.Request.Headers.TryGetValue(EXPECTED_HEADER, out var value))
            {
                if (!IPAddress.TryParse(value, out var ipAddress))
                    _logger.LogWarning(
                        $"""
                         Server configuration \"{nameof(IBlacklistSettings.IsBehindProxy)}\" is set to {true}, but value \"{value}\" of header \"{EXPECTED_HEADER}\" is not a valid IP address.
                         For security reasons, it is recommended to set this setting to {false} if your proxy doesn't support this header.
                         """
                        );
                else
                    return ipAddress;
            }
            else if (_blacklistSettings.Enabled)
            {
                _logger.LogWarning(
                    $"""
                    Server configuration \"{nameof(IBlacklistSettings.IsBehindProxy)}\" is set to {true}, but expected header \"{EXPECTED_HEADER}\" was not found.
                    For security reasons, it is recommended to set this setting to {false} if your proxy doesn't support this header.
                    """
                    );
            }
        }

        return context.Connection.RemoteIpAddress;
    }


    private async Task RespondUnauthorized(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

        await context.Response.CompleteAsync();
    }
}