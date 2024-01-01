using System;
using System.IO;
using System.Reflection;
using CommandLine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TinfoilWebServer.Booting;
using TinfoilWebServer.Logging.Console;
using TinfoilWebServer.Logging.File;
using TinfoilWebServer.Services;
using TinfoilWebServer.Services.FSChangeDetection;
using TinfoilWebServer.Services.JSON;
using TinfoilWebServer.Services.Middleware.Authentication;
using TinfoilWebServer.Services.Middleware.Blacklist;
using TinfoilWebServer.Services.Middleware.Fingerprint;
using TinfoilWebServer.Services.VirtualFS;
using TinfoilWebServer.Settings;
using TinfoilWebServer.Settings.ConfigModels;
using TinfoilWebServer.Utils;

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

            if (parserResult.Value.ComputeSha256Passwords)
            {
                PromptSha256Passwords();
                return 0;
            }

            var bootInfo = BuildBootInfo(parserResult.Value);

            var logFileFormatter = new LogFileFormatter();

            var webHostBuilder = new WebHostBuilder();
            webHostBuilder
                .SuppressStatusMessages(true)
                .ConfigureAppConfiguration(builder =>
                {
                    builder.AddJsonFile(bootInfo.ConfigFileFullPath, optional: true, reloadOnChange: RELOAD_CONFIG_ON_CHANGE);
                })
                .ConfigureLogging((ctx, loggingBuilder) =>
                {
                    var loggingConfig = ctx.Configuration.GetSection("Logging");
                    loggingBuilder
                        .AddConfiguration(loggingConfig)
                        .AddConsoleFormatter<CustomConsoleFormatter, CustomConsoleFormatterOptions>()
                        .AddConsole(options =>
                        {
                            if (options.FormatterName == null) // NOTE: not null when formatter name is specified in config file
                                options.FormatterName = nameof(CustomConsoleFormatter);
                        })
                        .AddFile(loggingConfig, options =>
                        {
                            options.FormatLogEntry = logFileFormatter.FormatLogEntry;
                        });
                })
                .ConfigureServices((ctx, services) =>
                {
                    services
                        .Configure<AppSettingsModel>(ctx.Configuration)
                        .Configure<FileFormatterOptions>(ctx.Configuration.GetSection("Logging").GetSection("File")
                            .GetSection("FormatterOptions"))
                        .AddSingleton<IBootInfo>(_ => bootInfo)

                        .AddSingleton<IAppSettings, AppSettings>()
                        .AddSingleton<ICacheSettings>(provider => provider.GetRequiredService<IAppSettings>().Cache)
                        .AddSingleton<IAuthenticationSettings>(provider =>
                            provider.GetRequiredService<IAppSettings>().Authentication)
                        .AddSingleton<IFingerprintsFilterSettings>(provider =>
                            provider.GetRequiredService<IAppSettings>().FingerprintsFilter)
                        .AddSingleton<IBlacklistSettings>(provider =>
                            provider.GetRequiredService<IAppSettings>().Blacklist)

                        .AddSingleton<IBlacklistMiddleware, BlacklistMiddleware>()
                        .AddSingleton<IBasicAuthMiddleware, BasicAuthMiddleware>()
                        .AddSingleton<IFingerprintMiddleware, FingerprintMiddleware>()

                        .AddSingleton<IJsonMerger, JsonMerger>()
                        .AddSingleton<IJsonSerializer, JsonSerializer>()
                        .AddSingleton<IFingerprintsFilteringManager, FingerprintsFilteringManager>()
                        .AddSingleton<IFingerprintsSerializer, FingerprintsSerializer>()
                        .AddSingleton<IBlacklistManager, BlacklistManager>()
                        .AddSingleton<IBlacklistSerializer, BlacklistSerializer>()
                        .AddSingleton<IRequestManager, RequestManager>()
                        .AddSingleton<IFileFilter, FileFilter>()
                        .AddSingleton<IVirtualItemFinder, VirtualItemFinder>()
                        .AddSingleton<ITinfoilIndexBuilder, TinfoilIndexBuilder>()
                        .AddSingleton<IVirtualFileSystemBuilder, VirtualFileSystemBuilder>()
                        .AddSingleton<IVirtualFileSystemRootProvider, VirtualFileSystemRootProvider>()
                        .AddSingleton<ICustomIndexManager, CustomIndexManager>()
                        .AddSingleton<IFileChangeHelper, FileChangeHelper>()
                        .AddSingleton<IDirectoryChangeHelper, DirectoryChangeHelper>()
                        .AddSingleton<IVFSPeriodicRefreshManager, VFSPeriodicRefreshManager>()
                        .AddSingleton<IVFSAutoRefreshManager, VFSAutoRefreshManager>();
                })
                .UseKestrel((ctx, options) =>
                {
                    options.Configure(ctx.Configuration.GetSection("Kestrel"), RELOAD_CONFIG_ON_CHANGE);
                })
                .UseStartup<Startup>();

            // Build Dependency Injection
            var webHost = webHostBuilder.Build();

            logger = webHost.Services.GetRequiredService<ILogger<Program>>();
            logger.LogWelcomeMessage();
            logger.LogBootInfo(bootInfo);
            logger.LogRelevantSettings(webHost.Services.GetRequiredService<IAppSettings>());
            logger.LogCurrentMachineInfo();

            // Hack because FileFormatterOptions can't be injected due to bad design of "NReco.Logging.File"
            logFileFormatter.Initialize(webHost.Services.GetRequiredService<IOptionsMonitor<FileFormatterOptions>>());

            AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            {
                var ex = eventArgs.ExceptionObject as Exception;
                logger.LogError(ex, $"An unhandled exception occurred: {ex?.Message ?? eventArgs.ExceptionObject}");
            };

            //===========================//
            //===> Starts the server <===//
            if (bootInfo.CmdOptions.RunAsWindowsService)
                webHost.RunAsService();
            else
                webHost.Run();
            //===> Starts the server <===//
            //===========================//

            logger.LogInformation("Server closing.");

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

    private static void PromptSha256Passwords()
    {
        Console.WriteLine(@"=> Compute SHA256 passwords hash <=");
        string? input;
        do
        {
            Console.WriteLine(@"Enter a password (CTRL+C to exit):");

            input = Console.ReadLine();
            if (input != null)
            {
                var hash = HashHelper.ComputeSha256(input);

                Console.WriteLine(@"Hash is:");
                Console.WriteLine(hash);
            }
        } while (input != null);
    }

    private static BootInfo BuildBootInfo(CmdOptions cmdOptions)
    {
        var bootInfo = new BootInfo
        {
            CmdOptions = cmdOptions,
        };

        var currentDirectory = cmdOptions.CurrentDirectory;
        if (!string.IsNullOrWhiteSpace(currentDirectory))
        {
            try
            {
                Environment.CurrentDirectory = currentDirectory;
            }
            catch (Exception ex)
            {
                bootInfo.Errors.Add($"Failed to change the current directory to \"{currentDirectory}\": {ex.Message}");
            }
        }

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
        bootInfo.ConfigFileFullPath = Path.GetFullPath(configFilePathRaw);

        return bootInfo;
    }

}