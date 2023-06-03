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
using TinfoilWebServer.Settings.ConfigModels;

namespace TinfoilWebServer;

public class Program
{
    private const bool RELOAD_CONFIG_ON_CHANGE = true;

    public static string ExpectedConfigFilePath { get; }

    static Program()
    {
        ExpectedConfigFilePath = InitExpectedConfigFilePath();
    }

    private static string InitExpectedConfigFilePath()
    {
        var currentAssemblyName = Assembly.GetExecutingAssembly().ManifestModule.Name;
        var currentAssemblyNameWithoutExt = Path.GetFileNameWithoutExtension(currentAssemblyName);
        return Path.GetFullPath($"{currentAssemblyNameWithoutExt}.config.json");
    }

    public static void Main(string[] args)
    {
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        IConfigurationRoot configRoot;
        var configFilePath = ExpectedConfigFilePath;
        try
        {
            configRoot = new ConfigurationBuilder()
                .AddJsonFile(configFilePath, optional: true, reloadOnChange: RELOAD_CONFIG_ON_CHANGE)
                .Build();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to build configuration: {ex.Message}{Environment.NewLine}Is \"{configFilePath}\" a valid JSON file?");
            Environment.ExitCode = 1;
            return;
        }

        var webHostBuilder = new WebHostBuilder();
        webHostBuilder
            .SuppressStatusMessages(true)
            .ConfigureLogging((hostingContext, loggingBuilder) =>
            {
                loggingBuilder
                    .AddConsoleFormatter<CustomConsoleFormatter, CustomConsoleFormatterOptions>(options => { })
                    .AddConfiguration(configRoot.GetSection("Logging"))
                    .AddConsole(options => options.FormatterName = nameof(CustomConsoleFormatter))
                    .AddFile(configRoot.GetSection("Logging"), options =>
                    {
                        options.FormatLogEntry = message =>
                        {
                            var exceptionMessage = "";
                            if (message.Exception != null) 
                                exceptionMessage += $"{Environment.NewLine}StackTrace{Environment.NewLine}{message.Exception.StackTrace}";

                            return $"{DateTime.Now}-{message.LogLevel}: {message.Message}{exceptionMessage}";
                        } ;
                    });
            })
            .ConfigureServices(services =>
            {
                services
                    .Configure<AppSettingsModel>(configRoot)
                    .AddSingleton<IBasicAuthMiddleware, BasicAuthMiddleware>()
                    .AddSingleton<IRequestManager, RequestManager>()
                    .AddSingleton<IFileFilter, FileFilter>()
                    .AddSingleton<IAppSettings, AppSettings>()
                    .AddSingleton<IAuthenticationSettings>(provider => provider.GetRequiredService<IAppSettings>().Authentication)
                    .AddSingleton<ICacheExpirationSettings>(provider => provider.GetRequiredService<IAppSettings>().CacheExpiration)
                    .AddSingleton<IVirtualItemFinder, VirtualItemFinder>()
                    .AddSingleton<IJsonSerializer, JsonSerializer>()
                    .AddSingleton<ITinfoilIndexBuilder, TinfoilIndexBuilder>()
                    .AddSingleton<IVirtualFileSystemBuilder, VirtualFileSystemBuilder>()
                    .AddSingleton<IVirtualFileSystemRootProvider, VirtualFileSystemRootProvider>();

            })
            .UseConfiguration(configRoot)
            .UseKestrel((ctx, options) =>
            {
                options.Configure(configRoot.GetSection("Kestrel"), RELOAD_CONFIG_ON_CHANGE);
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