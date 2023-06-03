﻿using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Logging;
using TinfoilWebServer.Services.Authentication;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer;

public class Startup
{
    /// <summary>
    /// WTF ASP.NET! This method is implicitly called by <see cref="WebHostBuilderExtensions.UseStartup{TStartup}(IWebHostBuilder)"/>
    /// </summary>
    /// <param name="app"></param>
    /// <param name="requestManager"></param>
    /// <param name="logger"></param>
    public void Configure(IApplicationBuilder app, IRequestManager requestManager, ILogger<Startup> logger)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version!;

        logger.LogInformation($"Welcome to Tinfoil Web Server v{version.Major}.{version.Minor}.{version.Build} (press CTRL+C to exit)");
        logger.LogInformation($"Server Host/IP:{GetCurrentComputerAddressesOrHosts().ToMultilineString()}");


        var configFilePath = Program.ExpectedConfigFilePath;
        if (File.Exists(configFilePath))
            logger.LogInformation($"Configuration file:{LogUtil.MultilineLogSpacing}\"{configFilePath}\" found");
        else
            logger.LogWarning($"Configuration file:{LogUtil.MultilineLogSpacing}\"{configFilePath}\" not found");

        app.UseMiddleware<IBasicAuthMiddleware>();
        app.ApplicationServices.GetRequiredService<IBasicAuthMiddleware>(); //Just to force initialization of middleware without waiting for first request
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