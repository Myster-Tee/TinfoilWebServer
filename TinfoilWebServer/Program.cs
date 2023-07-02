using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Logging;
using TinfoilWebServer.Logging.Console;
using TinfoilWebServer.Services;
using TinfoilWebServer.Services.Middleware.Authentication;
using TinfoilWebServer.Services.Middleware.BlackList;
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
        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        return Path.GetFullPath($"{assemblyName}.config.json");
    }

    public static void Main(string[] args)
    {
        ILogger<Program>? logger = null;
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
                        .AddConsole(options =>
                        {
                            if (options.FormatterName == null) // NOTE: not null when formatter name is specified in config file
                                options.FormatterName = nameof(CustomConsoleFormatter);
                        })
                        .AddFile(ctx.Configuration.GetSection("Logging"), options => options.FormatLogEntry = LogFileFormatter.FormatLogEntry);
                })
                .ConfigureServices((ctx, services) =>
                {
                    services
                        .Configure<AppSettingsModel>(ctx.Configuration)

                        .AddSingleton<IAuthenticationSettings>(provider => provider.GetRequiredService<IAppSettings>().Authentication)
                        .AddSingleton<ICacheExpirationSettings>(provider => provider.GetRequiredService<IAppSettings>().CacheExpiration)
                        .AddSingleton<IBlacklistSettings>(provider => provider.GetRequiredService<IAppSettings>().BlacklistSettings)

                        .AddSingleton<ISummaryInfoLogger, SummaryInfoLogger>()
                        .AddSingleton<IBlacklistMiddleware, BlacklistMiddleware>()
                        .AddSingleton<IBasicAuthMiddleware, BasicAuthMiddleware>()
                        .AddSingleton<IBlacklistManager, BlacklistManager>()
                        .AddSingleton<IBlacklistSerializer, BlacklistSerializer>()
                        .AddSingleton<IRequestManager, RequestManager>()
                        .AddSingleton<IFileFilter, FileFilter>()
                        .AddSingleton<IAppSettings, AppSettings>()
                        .AddSingleton<IVirtualItemFinder, VirtualItemFinder>()
                        .AddSingleton<IJsonSerializer, JsonSerializer>()
                        .AddSingleton<ITinfoilIndexBuilder, TinfoilIndexBuilder>()
                        .AddSingleton<IVirtualFileSystemBuilder, VirtualFileSystemBuilder>()
                        .AddSingleton<IVirtualFileSystemRootProvider, VirtualFileSystemRootProvider>()

                        .AddTransient<IFileChangeHelper, FileChangeHelper>();

                })
                .UseKestrel((ctx, options) =>
                {
                    options.Configure(ctx.Configuration.GetSection("Kestrel"), RELOAD_CONFIG_ON_CHANGE);
                })
                .UseStartup<Startup>();

            // Build Dependency Injection
            var webHost = webHostBuilder.Build();

            logger = webHost.Services.GetRequiredService<ILogger<Program>>();

            AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            {
                var ex = eventArgs.ExceptionObject as Exception;
                logger.LogError(ex, $"An unhandled exception occurred: {ex?.Message ?? eventArgs.ExceptionObject}");
            };

            var summaryInfoLogger = webHost.Services.GetRequiredService<ISummaryInfoLogger>();
            summaryInfoLogger.LogWelcomeMessage();
            summaryInfoLogger.LogRelevantSettings();
            summaryInfoLogger.LogCurrentMachineInfo();

            //===========================//
            //===> Starts the server <===//
            var runTask = webHost.RunAsync();
            //===> Starts the server <===//
            //===========================//

            summaryInfoLogger.LogListenedHosts();

            // Wait for server to shutdown
            runTask.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            if (logger != null)
                logger.LogError(ex, $"An unexpected error occurred: {ex.Message}");
            else
                Console.Error.WriteLine(
                    $"An unexpected error occurred: {ex.Message}{Environment.NewLine}" +
                    $"Exception Type: {ex.GetType().Name}{Environment.NewLine}" +
                    $"Stack Trace:{Environment.NewLine}" +
                    $"{ex.StackTrace}"
                    );
            Environment.ExitCode = 1;
        }
    }

}