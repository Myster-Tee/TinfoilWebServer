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

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var remoteIpAddress = GetRemoteIpAddress(context);
        if (remoteIpAddress == null)
        {
            _logger.LogWarning($"Incoming request \"{context.TraceIdentifier}\" rejected, no remote IP address found.");
            await RespondUnauthorized(context);
            return;
        }

        if (_blacklistManager.IsIpBlacklisted(remoteIpAddress))
        {
            _logger.LogInformation($"Incoming request \"{context.TraceIdentifier}\" rejected, IP address \"{remoteIpAddress}\" blacklisted.");
            await RespondUnauthorized(context);
            return;
        }

        _logger.LogInformation($"Incoming request \"{context.TraceIdentifier}\" from IP Address \"{remoteIpAddress}\".");

        await next.Invoke(context);

        if (context.Response.StatusCode is >= 401 and <= 403)
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
                    _logger.LogWarning($"Server configuration \"{nameof(IBlacklistSettings.IsBehindProxy)}\" is set to {true}, but value \"{value}\" of header \"{EXPECTED_HEADER}\" is not a valid IP address.{Environment.NewLine}" +
                                       $"For security reasons, it is recommended to set this setting to {false} if your proxy doesn't support this header.");
                else
                    return ipAddress;
            }
            else if(_blacklistSettings.Enabled)
            {
                _logger.LogWarning($"Server configuration \"{nameof(IBlacklistSettings.IsBehindProxy)}\" is set to {true}, but expected header \"{EXPECTED_HEADER}\" was not found.{Environment.NewLine}" +
                                   $"For security reasons, it is recommended to set this setting to {false} if your proxy doesn't support this header.");
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