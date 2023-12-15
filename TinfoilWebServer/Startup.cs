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
    /// WTF ASP.NET! This method is implicitly called by <see cref="WebHostBuilderExtensions.UseStartup{TStartup}(IWebHostBuilder)"/>
    /// </summary>
    /// <param name="app"></param>
    /// <param name="requestManager"></param>
    public void Configure(IApplicationBuilder app, IRequestManager requestManager)
    {
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

        app.Run(requestManager.OnRequest);
    }

}