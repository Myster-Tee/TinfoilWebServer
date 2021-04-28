using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer
{
    public class Startup
    {
        private static readonly string Spacing = $"{Environment.NewLine}        ";

        /// <summary>
        /// Welcome to ASP.NET, this method is implicitly called «.UseStartup<Startup>()»
        /// </summary>
        /// <param name="app"></param>
        /// <param name="requestManager"></param>
        /// <param name="logger"></param>
        /// <param name="appSettings"></param>
        public void Configure(IApplicationBuilder app, IRequestManager requestManager, ILogger<Startup> logger, IAppSettings appSettings)
        {
            logger.LogInformation($"Welcome to Tinfoil Web Server v{Assembly.GetExecutingAssembly().GetName().Version}");

            logger.LogInformation($"Served directories:{string.Join("", appSettings.ServedDirectories.Select(s => $"{Spacing}-> {s}"))}");

            logger.LogInformation($"Server Host/IP:{Spacing}{string.Join($"{Spacing}", GetCurrentComputerAddressesOrHosts())}");

            logger.LogInformation($"Tinfoil index type:{Spacing}{appSettings.IndexType}.");

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