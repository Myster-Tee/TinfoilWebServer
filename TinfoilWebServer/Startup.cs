using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Services;
using TinfoilWebServer.Services.Middleware.Authentication;
using TinfoilWebServer.Services.Middleware.Blacklist;
using TinfoilWebServer.Services.Middleware.Fingerprint;
using TinfoilWebServer.Utils;

namespace TinfoilWebServer;

public class Startup
{
    /// <summary>
    /// WTF ASP.NET! This method is implicitly called by <see cref="WebHostBuilderExtensions.UseStartup{TStartup}(IWebHostBuilder)"/>.
    /// This method is automatically called once <see cref="IWebHost"/> is ran and server is listening.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="logger"></param>
    /// <param name="server"></param>
    public void Configure(IApplicationBuilder app, ILogger<Startup> logger, IServer server)
    {
        // Method below shouldn't be invoked before host is ran, otherwise list of listened hosts is null
        logger.LogListenedHosts(server.Features.GetRequiredFeature<IServerAddressesFeature>());

        app
            .UseMiddleware<IBlacklistMiddleware>()
            .UseMiddleware<IBasicAuthMiddleware>()
            .UseMiddleware<IFingerprintMiddleware>(); // This middleware should be added after the authentication middleware

        app.ApplicationServices.GetRequiredService<IBasicAuthMiddleware>();                         // Just to force initialization without waiting for first request
        app.ApplicationServices.GetRequiredService<IFingerprintMiddleware>();                       // Just to force initialization without waiting for first request
        app.ApplicationServices.GetRequiredService<IBlacklistManager>().Initialize();
        app.ApplicationServices.GetRequiredService<IFingerprintsFilteringManager>().Initialize();
        app.ApplicationServices.GetRequiredService<IVirtualFileSystemRootProvider>().SafeRefresh(); // 1st refresh served files cache
        app.ApplicationServices.GetRequiredService<IVFSAutoRefreshManager>().Initialize();
        app.ApplicationServices.GetRequiredService<IVFSPeriodicRefreshManager>().Initialize();

        var requestManager = app.ApplicationServices.GetRequiredService<IRequestManager>();

        app.Run(requestManager.OnRequest);
    }
}