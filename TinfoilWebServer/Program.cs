using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Logging.Console;
using TinfoilWebServer.Services;
using TinfoilWebServer.Services.Authentication;
using TinfoilWebServer.Services.VirtualFS;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer;

public class Program
{

    public static string ExpectedConfigFilePath { get; }

    static Program()
    {
        ExpectedConfigFilePath = InitExpectedConfigFilePath();
    }

    private static string InitExpectedConfigFilePath()
    {
        var currentAssemblyName = Assembly.GetExecutingAssembly().ManifestModule.Name;
        var currentAssemblyNameWithoutExt = Path.GetFileNameWithoutExtension(currentAssemblyName);
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{currentAssemblyNameWithoutExt}.config.json");
    }

    public static void Main(string[] args)
    {

        IConfigurationRoot configRoot;
        var configFilePath = ExpectedConfigFilePath;
        try
        {
            configRoot = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile(configFilePath, optional: true, reloadOnChange: true)
                .Build();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to build configuration: {ex.Message}{Environment.NewLine}Is \"{configFilePath}\" a valid JSON file?");
            Environment.ExitCode = 1;
            return;
        }

        IAppSettings appSettings;
        try
        {
            appSettings = configRoot.LoadAppSettings();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load configuration from file \"{configFilePath}\": {ex.Message}");
            Environment.ExitCode = 1;
            return;
        }

        var webHostBuilder = new WebHostBuilder()
            .SuppressStatusMessages(true)
            .ConfigureLogging((hostingContext, loggingBuilder) =>
            {
                loggingBuilder
                    .AddConsoleFormatter<CustomConsoleFormatter, CustomConsoleFormatterOptions>(options => { })
                    .AddConfiguration(appSettings.LoggingConfig)
                    .AddConsole(options => options.FormatterName = nameof(CustomConsoleFormatter));
            })
            .ConfigureServices(services =>
            {
                services
                    .AddSingleton(appSettings)
                    .AddSingleton<IBasicAuthMiddleware, BasicAuthMiddleware>()
                    .AddSingleton<IRequestManager, RequestManager>()
                    .AddSingleton<IFileFilter, FileFilter>()
                    .AddSingleton<IVirtualItemFinder, VirtualItemFinder>()
                    .AddSingleton<IJsonSerializer, JsonSerializer>()
                    .AddSingleton<ITinfoilIndexBuilder, TinfoilIndexBuilder>()
                    .AddSingleton<IVirtualFileSystemBuilder, VirtualFileSystemBuilder>()
                    .AddSingleton<IVirtualFileSystemRootProvider, VirtualFileSystemRootProvider>();

            })
            .UseConfiguration(configRoot)
            .UseKestrel((ctx, options) =>
            {
                var kestrelConfig = appSettings.KestrelConfig;
                if (kestrelConfig != null)
                    options.Configure(kestrelConfig);
            })
            .UseStartup<Startup>();


        var webHost = webHostBuilder.Build();

        try
        {
            webHost.Run();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to run server: {ex.Message}");
            Environment.ExitCode = 1;
            return;
        }

    }


}