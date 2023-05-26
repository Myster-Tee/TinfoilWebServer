using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ElMariachi.Http.Header.Managed;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using TinfoilWebServer.HttpExtensions;
using TinfoilWebServer.Properties;
using TinfoilWebServer.Services;
using TinfoilWebServer.Services.VirtualFS;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer;

public class RequestManager : IRequestManager
{
    private readonly IAppSettings _appSettings;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IVirtualFileSystemProvider _virtualFileSystemProvider;
    private readonly ITinfoilIndexBuilder _tinfoilIndexBuilder;

    public RequestManager(
        IAppSettings appSettings,
        IFileFilter fileFilter,
        IJsonSerializer jsonSerializer,
        IVirtualFileSystemProvider virtualFileSystemProvider,
        ITinfoilIndexBuilder tinfoilIndexBuilder
        )
    {
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        _virtualFileSystemProvider = virtualFileSystemProvider ?? throw new ArgumentNullException(nameof(virtualFileSystemProvider));
        _tinfoilIndexBuilder = tinfoilIndexBuilder ?? throw new ArgumentNullException(nameof(tinfoilIndexBuilder));
    }

    public async Task OnRequest(HttpContext context)
    {

        var request = context.Request;


        var decodedRelPath = request.Path.Value!; // NOTE: good to read this article https://stackoverflow.com/questions/66471763/inconsistent-url-decoding-of-httprequest-path-in-asp-net-core

        if (string.Equals(decodedRelPath, "/favicon.ico", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.Body.WriteAsync(Resources.Favicon);
            return;
        }

        var rawUri = new Uri(request.GetEncodedUrl());
        var virtualItem = _virtualFileSystemProvider.Root.ReachItem(rawUri);
        var serverUrlRoot = rawUri.GetLeftPart(UriPartial.Authority);

        if (virtualItem == _virtualFileSystemProvider.Root && request.Method == "GET")
        {
            var tinfoilIndex = _tinfoilIndexBuilder.Build(serverUrlRoot, _virtualFileSystemProvider.Root, _appSettings.IndexType, _appSettings.MessageOfTheDay);

            var json = _jsonSerializer.Serialize(tinfoilIndex);

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(json, Encoding.UTF8);
        }
        else if ((request.Method is "GET" or "HEAD") && virtualItem is VirtualDirectory virtualDirectory)
        {
            var tinfoilIndex = _tinfoilIndexBuilder.Build(serverUrlRoot, virtualDirectory, _appSettings.IndexType, null);

            var json = _jsonSerializer.Serialize(tinfoilIndex);

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(json, Encoding.UTF8);
        }
        else if ((request.Method is "GET" or "HEAD") && virtualItem is VirtualFile virtualFile)
        {
            var rangeHeader = new RangeHeader
            {
                RawValue = request.Headers["range"]
            };

            var ranges = rangeHeader.Ranges;
            var range = ranges.Count == 1 ? ranges[0] : null;

            await context.Response.WriteFile(virtualFile.FullLocalPath, range: range);
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            await context.Response.CompleteAsync();
        }
    }

}