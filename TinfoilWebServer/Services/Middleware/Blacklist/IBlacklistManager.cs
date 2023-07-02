using System.Net;

namespace TinfoilWebServer.Services.Middleware.BlackList;

public interface IBlacklistManager
{
    /// <summary>
    /// Determines if given IP is blacklisted
    /// </summary>
    /// <param name="ipAddress"></param>
    /// <returns></returns>
    bool IsIpBlacklisted(IPAddress ipAddress);

    /// <summary>
    /// Reports the IP as unauthorized
    /// </summary>
    /// <param name="ipAddress"></param>
    void ReportIpUnauthorized(IPAddress ipAddress);

    /// <summary>
    /// Reports the IP as unauthorized
    /// </summary>
    /// <param name="ipAddress"></param>
    void ReportIpAuthorized(IPAddress ipAddress);

    /// <summary>
    /// Initializes manager
    /// </summary>
    void Initialize();
};