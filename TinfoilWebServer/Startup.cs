using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TinfoilWebServer.Services;
using TinfoilWebServer.Services.Authentication;

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
        app.UseMiddleware<IBasicAuthMiddleware>();
        app.ApplicationServices.GetRequiredService<IBasicAuthMiddleware>();             //Just to force initialization without waiting for first request
        app.ApplicationServices.GetRequiredService<IVirtualFileSystemRootProvider>().Initialize();   //Just to force initialization without waiting for first request
        app.Run(requestManager.OnRequest);
    }

}