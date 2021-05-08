using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElMariachi.Http.Header.Managed;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using TinfoilWebServer.HttpExtensions;
using TinfoilWebServer.Properties;
using TinfoilWebServer.Services;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer
{
    public class RequestManager : IRequestManager
    {
        private readonly IAppSettings _appSettings;
        private readonly ITinfoilIndexBuilder _tinfoilIndexBuilder;
        private readonly IFileFilter _fileFilter;
        private readonly IPhysicalPathConverter _physicalPathConverter;
        private readonly IServedDirAliasMap _servedDirAliasMap;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IUrlCombinerFactory _urlCombinerFactory;

        public RequestManager(
            IAppSettings appSettings, ITinfoilIndexBuilder tinfoilIndexBuilder,
            IFileFilter fileFilter, IPhysicalPathConverter physicalPathConverter,
            IServedDirAliasMap servedDirAliasMap, IJsonSerializer jsonSerializer,
            IUrlCombinerFactory urlCombinerFactory)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _tinfoilIndexBuilder = tinfoilIndexBuilder ?? throw new ArgumentNullException(nameof(tinfoilIndexBuilder));
            _fileFilter = fileFilter ?? throw new ArgumentNullException(nameof(fileFilter));
            _physicalPathConverter = physicalPathConverter ?? throw new ArgumentNullException(nameof(physicalPathConverter));
            _servedDirAliasMap = servedDirAliasMap ?? throw new ArgumentNullException(nameof(servedDirAliasMap));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _urlCombinerFactory = urlCombinerFactory ?? throw new ArgumentNullException(nameof(urlCombinerFactory));
        }

        public async Task OnRequest(HttpContext context)
        {
            var request = context.Request;

            var rootUrlCombiner = _urlCombinerFactory.Create(new Uri(context.Request.GetEncodedUrl(), UriKind.Absolute));

            var decodedRelPath = request.Path.Value!; // NOTE: good to read this article https://stackoverflow.com/questions/66471763/inconsistent-url-decoding-of-httprequest-path-in-asp-net-core

            if (string.Equals(decodedRelPath, "/favicon.ico", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 200;
                await context.Response.Body.WriteAsync(Resources.Favicon);
                return;
            }

            var physicalPath = _physicalPathConverter.Convert(decodedRelPath, out var isRoot);
            if (isRoot)
            {

                var dirs = _servedDirAliasMap.Select(dirWithAlias => new Dir
                {
                    Path = dirWithAlias.DirPath,
                    CorrespondingUrl = rootUrlCombiner.CombineLocalPath(dirWithAlias.Alias)
                }).ToArray();

                var tinfoilIndex = _tinfoilIndexBuilder.Build(dirs, _appSettings.IndexType, _appSettings.MessageOfTheDay);

                var json = _jsonSerializer.Serialize(tinfoilIndex);

                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsync(json, Encoding.UTF8);
            }
            else if (Directory.Exists(physicalPath) && request.Method == "GET" || request.Method == "HEAD")
            {
                var tinfoilIndex = _tinfoilIndexBuilder.Build(new[]{new Dir
                {
                    CorrespondingUrl = rootUrlCombiner.BaseAbsUrl,
                    Path = physicalPath!,
                }}, _appSettings.IndexType, null);

                var json = _jsonSerializer.Serialize(tinfoilIndex);

                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsync(json, Encoding.UTF8);
            }
            else if (_fileFilter.IsFileAllowed(physicalPath) && File.Exists(physicalPath) && request.Method == "GET" || request.Method == "HEAD")
            {
                var rangeHeader = new RangeHeader
                {
                    RawValue = request.Headers["range"]
                };

                var ranges = rangeHeader.Ranges;
                var range = ranges.Count == 1 ? ranges[0] : null;

                await context.Response.WriteFile(physicalPath!, range: range);
            }
            else
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("<!DOCTYPE html><http><head><title>Oops!</title></head><body style='text-align:center'>404<br>Not found...</body></html>");
            }
        }

    }
}