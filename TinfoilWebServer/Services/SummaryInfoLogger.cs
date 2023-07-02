using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Logging;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services;

public class SummaryInfoLogger : ISummaryInfoLogger
{
    private readonly ILogger<SummaryInfoLogger> _logger;
    private readonly IAppSettings _appSettings;
    private readonly IServer _server;

    public SummaryInfoLogger(ILogger<SummaryInfoLogger> logger, IAppSettings appSettings, IServer server)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    public void LogWelcomeMessage()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version!;

        _logger.LogInformation($"Welcome to Tinfoil Web Server v{version.Major}.{version.Minor}.{version.Build} (press CTRL+C to exit)");
    }

    public void LogRelevantSettings()
    {
        var configFilePath = Program.ExpectedConfigFilePath;
        if (File.Exists(configFilePath))
            _logger.LogInformation($"Configuration file found at location \"{configFilePath}\".");
        else
            _logger.LogWarning($"Configuration file not found at location \"{configFilePath}\".");


        var sb = new StringBuilder();
        sb.AppendLine($"Current configuration:");

        sb.AppendLine();

        sb.AppendLine($"- Served directories:{_appSettings.ServedDirectories.ToMultilineString()}");

        sb.AppendLine($"- Strip directory names: {_appSettings.StripDirectoryNames}");

        sb.AppendLine($"- Serve empty directories: {_appSettings.ServeEmptyDirectories}");

        sb.AppendLine($"- Allowed extensions:{_appSettings.AllowedExt.ToMultilineString()}");

        sb.AppendLine($"- Message of the day:");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}{_appSettings.MessageOfTheDay}");

        sb.AppendLine($"- Extra repositories:{_appSettings.ExtraRepositories.ToMultilineString()}");

        var cacheExpiration = _appSettings.CacheExpiration;
        sb.AppendLine($"- Cache expiration:");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}Enabled: {cacheExpiration.Enabled}");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}Expiration delay: {cacheExpiration.ExpirationDelay}");

        var authentication = _appSettings.Authentication;
        sb.AppendLine($"- Authentication:");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}Enabled: {authentication.Enabled}");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}Web browser auth enabled: {authentication.WebBrowserAuthEnabled}");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}Nb allowed users: {authentication.Users.Count}");

        var blacklist = _appSettings.BlacklistSettings;
        sb.AppendLine($"- Blacklist:");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}Enabled: {blacklist.Enabled}");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}Maximum consecutive failed authentication(s): {blacklist.MaxConsecutiveFailedAuth}");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}File path: {blacklist.FilePath}");

        _logger.LogInformation(sb.ToString());
    }

    public void LogCurrentMachineInfo()
    {
        _logger.LogInformation($"Current machine Host/IP:{GetCurrentComputerAddressesOrHosts().ToMultilineString()}");
    }

    public void LogListenedHosts()
    {
        var listenedAddresses = _server.Features.Get<IServerAddressesFeature>();
        _logger.LogInformation($"Listened addresses:{listenedAddresses?.Addresses.ToMultilineString()}");
    }


    private static IEnumerable<string> GetCurrentComputerAddressesOrHosts()
    {
        yield return Dns.GetHostName();

        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up)
                continue;
            foreach (var ip in networkInterface.GetIPProperties().UnicastAddresses)
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