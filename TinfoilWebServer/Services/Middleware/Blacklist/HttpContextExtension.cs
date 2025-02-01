using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace TinfoilWebServer.Services.Middleware.Blacklist;

public static class HttpContextExtension
{
    private const string DISABLE_BLACKLISTING_KEY = "DO_NOT_BLACKLIST";

    /// <summary>
    /// Disable blacklisting for the current request
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public static bool DisableBlacklisting(this HttpContext httpContext)
    {
        return httpContext.Items.TryAdd(DISABLE_BLACKLISTING_KEY, null);
    }

    /// <summary>
    /// Enable blacklisting for the current request
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public static bool EnableBlacklisting(this HttpContext httpContext)
    {
        return httpContext.Items.Remove(DISABLE_BLACKLISTING_KEY);
    }

    /// <summary>
    /// Check if blacklisting is disabled for the current request
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public static bool IsBlacklistingDisabled(this HttpContext httpContext)
    {
        return httpContext.Items.ContainsKey(DISABLE_BLACKLISTING_KEY);
    }

}