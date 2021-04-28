using System;
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

        static Program()
        {
            CurrentDirectory = Directory.GetCurrentDirectory();
        }

        public static void Main(string[] args)
        {
            Console.WriteLine(CurrentDirectory);

            var configRoot = new ConfigurationBuilder()
                .SetBasePath(CurrentDirectory)
                .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                .Build();

            var appSettings = AppSettingsLoader.Load(configRoot);

            //Host.CreateDefaultBuilder(args)

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
                        .AddSingleton<ITinfoilIndexBuilder, TinfoilIndexBuilder>()
                        .AddSingleton<IServedDirAliasMapper, ServedDirAliasMapper>()
                        .AddSingleton<IFileFilter, FileFilter>();
                })
                .UseConfiguration(configRoot)
                .UseKestrel((ctx, options) =>
                {
                    options.Configure(appSettings.KestrelConfig);
                })
                //.UseContentRoot(appSettings.ServedDirectories)
                .UseStartup<Startup>();

            webHostBuilder.Build().Run();
        }


    }
}