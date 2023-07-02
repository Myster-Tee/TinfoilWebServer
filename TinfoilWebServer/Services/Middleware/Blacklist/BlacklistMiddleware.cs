using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TinfoilWebServer.Services.Middleware.BlackList;

public class BlacklistMiddleware : IBlacklistMiddleware
{
    private readonly IBlacklistManager _blacklistManager;
    private readonly ILogger<BlacklistMiddleware> _logger;

    public BlacklistMiddleware(IBlacklistManager blacklistManager, ILogger<BlacklistMiddleware> logger)
    {
        _blacklistManager = blacklistManager ?? throw new ArgumentNullException(nameof(blacklistManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var remoteIpAddress = context.Connection.RemoteIpAddress;
        if (remoteIpAddress == null)
        {
            _logger.LogWarning($"Incoming request \"{context.TraceIdentifier}\" rejected, no remote IP address found.");
            await RespondUnauthorized(context);
            return;
        }

        if (_blacklistManager.IsIpBlacklisted(remoteIpAddress))
        {
            _logger.LogWarning($"Incoming request \"{context.TraceIdentifier}\" rejected, IP address \"{remoteIpAddress}\" blacklisted.");
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


    private async Task RespondUnauthorized(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

        await context.Response.CompleteAsync();
    }
}