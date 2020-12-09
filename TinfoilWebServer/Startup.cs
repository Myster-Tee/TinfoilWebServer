using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, IRequestManager requestManager, ILogger<Startup> logger, IAppSettings appSettings)
        {
            logger.LogInformation($"Served directory: {appSettings.ServedDirectory}");

            logger.LogInformation($"Server Host/IP: " + string.Join(", ", GetCurrentComputerAddressesOrHosts()));

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