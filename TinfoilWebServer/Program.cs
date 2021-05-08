using System;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.ConsoleLogging;
using TinfoilWebServer.Services;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer
{
    public class Program
    {
        public static string CurrentDirectory { get; }
        public static string ConfigFileName { get; }

        static Program()
        {
            CurrentDirectory = Directory.GetCurrentDirectory();
            ConfigFileName = InitConfigFileName();
        }

        private static string InitConfigFileName()
        {
            var currentExeWithoutExt = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess()?.MainModule?.FileName) ?? "TinfoilWebServer";
            return $"{currentExeWithoutExt}.config.json";
        }

        public static void Main(string[] args)
        {
            const string? CONFIG_FILE_NAME = "TinfoilWebServer.config.json";

            IConfigurationRoot configRoot;
            try
            {
                configRoot = new ConfigurationBuilder()
                    .SetBasePath(CurrentDirectory)
                    .AddJsonFile(CONFIG_FILE_NAME, optional: true, reloadOnChange: true)
                    .Build();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to build configuration: {ex.Message}{Environment.NewLine}Is «{CONFIG_FILE_NAME}» a valid JSON file?");
                Environment.ExitCode = 1;
                return;
            }

            IAppSettings? appSettings;
            try
            {
                appSettings = AppSettingsLoader.Load(configRoot, out var errors);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load configuration from file «{CONFIG_FILE_NAME}»: {ex.Message}");
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
                        .AddSingleton<IRequestManager, RequestManager>()
                        .AddSingleton<IServedDirAliasMap, ServedDirAliasMap>()
                        .AddSingleton<IPhysicalPathConverter, PhysicalPathConverter>()
                        .AddSingleton<IFileFilter, FileFilter>()
                        .AddSingleton<IUrlCombinerFactory, UrlCombinerFactory>()
                        .AddSingleton<IJsonSerializer, JsonSerializer>()
                        .AddSingleton<ITinfoilIndexBuilder, TinfoilIndexBuilder>();

                })
                .UseConfiguration(configRoot)
                .UseKestrel((ctx, options) =>
                {
                    options.Configure(appSettings.KestrelConfig);
                })
                .UseStartup<Startup>();

            webHostBuilder.Build().Run();
        }


    }
}