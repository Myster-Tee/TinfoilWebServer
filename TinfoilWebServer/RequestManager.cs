using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TinfoilWebServer.HttpExtensions;
using TinfoilWebServer.Properties;
using TinfoilWebServer.Services;
using TinfoilWebServer.Services.VirtualFS;

namespace TinfoilWebServer;

public class RequestManager : IRequestManager
{
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ITinfoilIndexBuilder _tinfoilIndexBuilder;
    private readonly IVirtualItemFinder _virtualItemFinder;

    public RequestManager(
        IJsonSerializer jsonSerializer,
        ITinfoilIndexBuilder tinfoilIndexBuilder,
        IVirtualItemFinder virtualItemFinder
        )
    {
        _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        _tinfoilIndexBuilder = tinfoilIndexBuilder ?? throw new ArgumentNullException(nameof(tinfoilIndexBuilder));
        _virtualItemFinder = virtualItemFinder ?? throw new ArgumentNullException(nameof(virtualItemFinder));
    }

    public async Task OnRequest(HttpContext context)
    {
        var request = context.Request;

        var decodedRelPath = request.Path.Value ?? ""; // NOTE: good to read this article https://stackoverflow.com/questions/66471763/inconsistent-url-decoding-of-httprequest-path-in-asp-net-core

        if (string.Equals(decodedRelPath, "/favicon.ico", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.Body.WriteAsync(Resources.Favicon);
            return;
        }

        var virtualItem = _virtualItemFinder.Find(request.Path);

        if ((request.Method is "GET" or "HEAD") && virtualItem is VirtualDirectory virtualDirectory)
        {
            var tinfoilIndex = _tinfoilIndexBuilder.Build(virtualDirectory);

            var json = _jsonSerializer.Serialize(tinfoilIndex);

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(json, Encoding.UTF8);
        }
        else if ((request.Method is "GET" or "HEAD") && virtualItem is VirtualFile virtualFile)
        {
            var rangeHeader = request.GetTypedHeaders().Range;

            await context.Response.WriteFile(virtualFile.FullLocalPath, rangeHeader: rangeHeader);
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            await context.Response.CompleteAsync();
        }
    }

}