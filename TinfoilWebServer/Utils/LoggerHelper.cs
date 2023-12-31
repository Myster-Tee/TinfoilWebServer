using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Booting;
using TinfoilWebServer.Logging;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Utils;

/// <summary>
/// Helper for logging various information
/// </summary>
public static class LoggerHelper
{

    public static void LogWelcomeMessage(this ILogger logger)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version!;
        logger.LogInformation($"Welcome to Tinfoil Web Server v{version.Major}.{version.Minor}.{version.Build} (press CTRL+C to exit)");
    }

    public static void LogBootInfo(this ILogger logger, IBootInfo bootInfo)
    {
        if (bootInfo.CmdOptions.RunAsWindowsService)
            logger.LogInformation($"Server running as a Windows service.");

        var configFilePath = bootInfo.ConfigFileFullPath;
        if (File.Exists(configFilePath))
            logger.LogInformation($"Configuration file found at location \"{configFilePath}\".");
        else
            logger.LogWarning($"Configuration file not found at location \"{configFilePath}\".");

        if (bootInfo.Errors.Count > 0)
        {
            var errorsStr = string.Join(Environment.NewLine, bootInfo.Errors.Select(e => $"-> {e}"));

            logger.LogError(
                $"""
                Some initialization errors occurred:
                {errorsStr}
                """
                );
        }

        logger.LogInformation($"Current directory is \"{Environment.CurrentDirectory}\".");
    }

    public static void LogRelevantSettings(this ILogger logger, IAppSettings appSettings)
    {

        var sb = new StringBuilder();
        sb.AppendLine($"Current configuration:");

        sb.AppendLine();

        sb.AppendLine($"- Served directories:{appSettings.ServedDirectories.Select(d => d.FullName).ToMultilineString()}");

        sb.AppendLine($"- Strip directory names: {appSettings.StripDirectoryNames}");

        sb.AppendLine($"- Serve empty directories: {appSettings.ServeEmptyDirectories}");

        sb.AppendLine($"- Allowed extensions:{appSettings.AllowedExt.ToMultilineString()}");

        sb.AppendLine($"- Message of the day:");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}{appSettings.MessageOfTheDay}");

        var cache = appSettings.Cache;
        sb.AppendLine($"- Cache:");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}Auto detect file changes: {cache.AutoDetectChanges}");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}Forced refresh delay: {cache.PeriodicRefreshDelay}");

        var authentication = appSettings.Authentication;
        sb.AppendLine($"- Authentication:");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}Enabled: {authentication.Enabled}");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}Web Browser auth enabled: {authentication.WebBrowserAuthEnabled}");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}Nb allowed users: {authentication.Users.Count}");

        var fingerprintsFilter = appSettings.FingerprintsFilter;
        sb.AppendLine($"- Fingerprints filter:");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}Enabled: {fingerprintsFilter.Enabled}");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}Allowed fingerprints file: {fingerprintsFilter.FingerprintsFilePath}");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}Max global fingerprints allowed: {fingerprintsFilter.MaxFingerprints}");

        var blacklist = appSettings.Blacklist;
        sb.AppendLine($"- Blacklist:");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}Enabled: {blacklist.Enabled}");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}File path: {blacklist.FilePath}");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}Maximum consecutive failed authentication(s): {blacklist.MaxConsecutiveFailedAuth}");
        sb.AppendLine($"{LogUtil.INDENT_SPACES}Is behind proxy: {blacklist.IsBehindProxy}");

        logger.LogInformation(sb.ToString());

    }

    public static void LogCurrentMachineInfo(this ILogger logger)
    {
        logger.LogInformation($"Current machine Host/IP:{GetCurrentComputerAddressesOrHosts().ToMultilineString()}");
    }

    public static void LogListenedHosts(this ILogger logger, IServerAddressesFeature serverAddressesFeature)
    {
        logger.LogInformation($"Listened addresses:{serverAddressesFeature?.Addresses.ToMultilineString()}");
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