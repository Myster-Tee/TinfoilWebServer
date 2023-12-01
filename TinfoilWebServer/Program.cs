using System;
using System.IO;
using System.Reflection;
using CommandLine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Booting;
using TinfoilWebServer.Logging;
using TinfoilWebServer.Logging.Console;
using TinfoilWebServer.Services;
using TinfoilWebServer.Services.FSChangeDetection;
using TinfoilWebServer.Services.JSON;
using TinfoilWebServer.Services.Middleware.Authentication;
using TinfoilWebServer.Services.Middleware.Blacklist;
using TinfoilWebServer.Services.VirtualFS;
using TinfoilWebServer.Settings;
using TinfoilWebServer.Settings.ConfigModels;

namespace TinfoilWebServer;

public class Program
{

    public static int Main(string[] args)
    {
        const bool RELOAD_CONFIG_ON_CHANGE = true;
        ILogger<Program>? logger = null;

        try
        {
            var parserResult = Parser.Default.ParseArguments<CmdOptions>(args).WithParsed(_ => { });
            if (parserResult.Tag == ParserResultType.NotParsed)
                return 2;

            var bootInfo = BuildBootInfo(parserResult.Value);

            var webHostBuilder = new WebHostBuilder();
            webHostBuilder
                .SuppressStatusMessages(true)
                .ConfigureAppConfiguration(builder =>
                {
                    builder.AddJsonFile(bootInfo.ConfigFileFullPath, optional: true, reloadOnChange: RELOAD_CONFIG_ON_CHANGE);
                })
                .ConfigureLogging((ctx, loggingBuilder) =>
                {
                    loggingBuilder
                        .AddConfiguration(ctx.Configuration.GetSection("Logging"))
                        .AddConsoleFormatter<CustomConsoleFormatter, CustomConsoleFormatterOptions>(_ => { })
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
                        .AddSingleton<IBootInfo>(_ => bootInfo)

                        .AddSingleton<IAppSettings, AppSettings>()
                        .AddSingleton<IAuthenticationSettings>(provider => provider.GetRequiredService<IAppSettings>().Authentication)
                        .AddSingleton<IBlacklistSettings>(provider => provider.GetRequiredService<IAppSettings>().BlacklistSettings)

                        .AddSingleton<ISummaryInfoLogger, SummaryInfoLogger>()
                        .AddSingleton<IBlacklistMiddleware, BlacklistMiddleware>()
                        .AddSingleton<IBasicAuthMiddleware, BasicAuthMiddleware>()
                        .AddSingleton<IBlacklistManager, BlacklistManager>()
                        .AddSingleton<IBlacklistSerializer, BlacklistSerializer>()
                        .AddSingleton<IRequestManager, RequestManager>()
                        .AddSingleton<IJsonMerger, JsonMerger>()
                        .AddSingleton<IFileFilter, FileFilter>()
                        .AddSingleton<IVirtualItemFinder, VirtualItemFinder>()
                        .AddSingleton<IJsonSerializer, JsonSerializer>()
                        .AddSingleton<ITinfoilIndexBuilder, TinfoilIndexBuilder>()
                        .AddSingleton<IVirtualFileSystemBuilder, VirtualFileSystemBuilder>()
                        .AddSingleton<IVirtualFileSystemRootProvider, VirtualFileSystemRootProvider>()
                        .AddSingleton<ICustomIndexManager, CustomIndexManager>()
                        .AddSingleton<IFileChangeHelper, FileChangeHelper>()
                        .AddSingleton<IDirectoryChangeHelper, DirectoryChangeHelper>();
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

            return 0;
        }
        catch (Exception ex)
        {
            if (logger != null)
                logger.LogError(ex, $"An unexpected error occurred: {ex.Message}");
            else
                Console.Error.WriteLine(
                     $"""
                      An unexpected error occurred: {ex.Message}
                      Exception Type: {ex.GetType().Name}
                      Stack Trace:
                      {ex.StackTrace}
                      """
                    );
            return 1;
        }
    }

    private static IBootInfo BuildBootInfo(CmdOptions cmdOptions)
    {
        string configFilePathRaw;
        if (cmdOptions.ConfigFilePath != null)
        {
            configFilePathRaw = cmdOptions.ConfigFilePath;
        }
        else
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            configFilePathRaw = $"{assemblyName}.config.json";
        }

        return new BootInfo
        {
            CmdOptions = cmdOptions,
            ConfigFileFullPath = Path.GetFullPath(configFilePathRaw)
        };
    }

}

