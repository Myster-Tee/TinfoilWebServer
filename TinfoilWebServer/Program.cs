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

    public static string ExpectedConfigFilePath { get; private set; } = "";


    private static string InitExpectedConfigFilePath()
    {
        var currentAssemblyName = Assembly.GetExecutingAssembly().ManifestModule.Name;
        var currentAssemblyNameWithoutExt = Path.GetFileNameWithoutExtension(currentAssemblyName);
        return Path.GetFullPath($"{currentAssemblyNameWithoutExt}.config.json");
    }

    public static void Main(string[] args)
    {
        try
        {
            // Change current application directory so that paths of config file and log file are relative to application directory
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            ExpectedConfigFilePath = InitExpectedConfigFilePath();


            var webHostBuilder = new WebHostBuilder();
            webHostBuilder
                .SuppressStatusMessages(true)
                .ConfigureAppConfiguration(builder =>
                {
                    builder.AddJsonFile(ExpectedConfigFilePath, optional: true, reloadOnChange: RELOAD_CONFIG_ON_CHANGE);
                })
                .ConfigureLogging((ctx, loggingBuilder) =>
                {
                    loggingBuilder
                        .AddConsoleFormatter<CustomConsoleFormatter, CustomConsoleFormatterOptions>(options => { })
                        .AddConfiguration(ctx.Configuration.GetSection("Logging"))
                        .AddConsole(options => options.FormatterName = nameof(CustomConsoleFormatter))
                        .AddFile(ctx.Configuration.GetSection("Logging"), options =>
                        {
                            options.FormatLogEntry = message =>
                            {
                                var exceptionMessage = "";
                                if (message.Exception != null)
                                    exceptionMessage += $"{Environment.NewLine}StackTrace{Environment.NewLine}{message.Exception.StackTrace}";

                                return $"{DateTime.Now}-{message.LogLevel}: {message.Message}{exceptionMessage}";
                            };
                        });
                })
                .ConfigureServices((ctx, services) =>
                {
                    services
                        .Configure<AppSettingsModel>(ctx.Configuration)
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
                .UseKestrel((ctx, options) =>
                {
                    options.Configure(ctx.Configuration.GetSection("Kestrel"), RELOAD_CONFIG_ON_CHANGE);
                })
                .UseStartup<Startup>();

            var webHost = webHostBuilder.Build();


            webHost.Run();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"An unexpected exception occurred: {ex.Message}");
            Environment.ExitCode = 1;
            return;
        }

    }


}