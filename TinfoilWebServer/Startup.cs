using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Logging;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer
{
    public class Startup
    {

        /// <summary>
        /// WTF ASP.NET, this method is implicitly called «.UseStartup<Startup>()»
        /// </summary>
        /// <param name="app"></param>
        /// <param name="requestManager"></param>
        /// <param name="logger"></param>
        /// <param name="appSettings"></param>
        public void Configure(IApplicationBuilder app, IRequestManager requestManager, ILogger<Startup> logger, IAppSettings appSettings)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version!;

            logger.LogInformation($"Welcome to Tinfoil Web Server v{version.Major}.{version.Minor}.{version.Build} (press CTRL+C to exit)");

            logger.LogInformation($"Server Host/IP:{GetCurrentComputerAddressesOrHosts().ToMultilineString()}");

            logger.LogInformation($"Served directories:{appSettings.ServedDirectories.ToMultilineString()}");

            foreach (var servedDirectory in appSettings.ServedDirectories)
            {
                if (!Directory.Exists(servedDirectory))
                    logger.LogWarning($"Directory «{servedDirectory}» not found!");
            }

            logger.LogInformation($"Allowed extensions:{appSettings.AllowedExt.ToMultilineString()}");

            logger.LogInformation($"Tinfoil index type:{LogUtil.MultilineLogSpacing}{appSettings.IndexType}");

            logger.LogInformation($"Cache index expiration:{LogUtil.MultilineLogSpacing}{appSettings.CacheExpiration}");


            app.Run(requestManager.OnRequest);
        }

        private static IEnumerable<string> GetCurrentComputerAddressesOrHosts()
        {
            yield return Dns.GetHostName();

            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up)
                    continue;
                foreach (UnicastIPAddressInformation ip in networkInterface.GetIPProperties().UnicastAddresses)
                {
                    var address = ip.Address;
                    if (address.AddressFamily == AddressFamily.InterNetwork /*|| address.AddressFamily == AddressFamily.InterNetworkV6*/)
                    {
                        yield return address.ToString();
                    }
                }
            }
        }

    }
}