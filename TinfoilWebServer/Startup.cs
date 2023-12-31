using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TinfoilWebServer.Services;
using TinfoilWebServer.Services.Middleware.Authentication;
using TinfoilWebServer.Services.Middleware.Blacklist;
using TinfoilWebServer.Services.Middleware.Fingerprint;

namespace TinfoilWebServer;

public class Startup
{

    /// <summary>
    /// WTF ASP.NET! This method is implicitly called by <see cref="WebHostBuilderExtensions.UseStartup{TStartup}(IWebHostBuilder)"/>.
    /// This method is automatically called once <see cref="IWebHost"/> is ran and server is listening.
    /// </summary>
    /// <param name="app"></param>
    public void Configure(IApplicationBuilder app)
    {
        var summaryInfoLogger = app.ApplicationServices.GetRequiredService<ISummaryInfoLogger>();
        summaryInfoLogger.LogListenedHosts(); // This method shouldn't be invoked before host is ran

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