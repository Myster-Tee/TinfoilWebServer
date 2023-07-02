using System.Collections.Generic;
using System.IO;
using System.Net;

namespace TinfoilWebServer.Services.Middleware.BlackList;

public interface IBlacklistSerializer
{
    /// <summary>
    /// Serialize specified IPs to file.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="ipAddresses"></param>
    void Serialize(string filePath, IReadOnlySet<IPAddress> ipAddresses);

    /// <summary>
    /// Deserialize IPs from specified file.
    /// Caller should ensure file exists.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="ipAddresses"></param>
    /// <exception cref="FileNotFoundException"></exception>
    void Deserialize(string filePath, ISet<IPAddress> ipAddresses);

}