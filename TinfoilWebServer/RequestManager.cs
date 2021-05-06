using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
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

        public RequestManager(IAppSettings appSettings, ITinfoilIndexBuilder tinfoilIndexBuilder, IFileFilter fileFilter, IPhysicalPathConverter physicalPathConverter, IServedDirAliasMap servedDirAliasMap)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _tinfoilIndexBuilder = tinfoilIndexBuilder ?? throw new ArgumentNullException(nameof(tinfoilIndexBuilder));
            _fileFilter = fileFilter ?? throw new ArgumentNullException(nameof(fileFilter));
            _physicalPathConverter = physicalPathConverter ?? throw new ArgumentNullException(nameof(physicalPathConverter));
            _servedDirAliasMap = servedDirAliasMap;
        }

        public async Task OnRequest(HttpContext context)
        {
            var request = context.Request;

            var decodedPath = request.Path.Value;
            var encodedPath = request.Path.ToUriComponent();


            if (string.Equals(decodedPath, "/favicon.ico", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 200;
                await context.Response.Body.WriteAsync(Resources.Favicon);
                return;
            }

            var physicalPath = _physicalPathConverter.Convert(decodedPath, out var isRoot);
            if (isRoot)
            {
                var url = request.GetEncodedUrl();

                var dirs = _servedDirAliasMap.Select(dirWithAlias => new Dir
                {
                    Path = dirWithAlias.DirPath,
                    CorrespondingUrl = new Uri(url + WebUtility.UrlDecode(dirWithAlias.Alias))
                }).ToArray();

                var tinfoilIndex = _tinfoilIndexBuilder.Build(dirs, _appSettings.IndexType, _appSettings.MessageOfTheDay);

                var json = JsonSerializer.Serialize(tinfoilIndex, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // NOTE: required to avoid escaping of some special chars like '+', '&', etc. (See https://docs.microsoft.com/fr-fr/dotnet/standard/serialization/system-text-json-character-encoding for more information)
                });

                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsync(json, Encoding.UTF8);
            }
            else if (Directory.Exists(physicalPath) && request.Method == "GET" || request.Method == "HEAD")
            {
                var tinfoilIndex = _tinfoilIndexBuilder.Build(new[]{new Dir
                {
                    CorrespondingUrl = new Uri($"{request.Scheme}://{request.Host}{encodedPath}"),
                    Path = physicalPath!,
                }}, _appSettings.IndexType, null);

                var json = JsonSerializer.Serialize(tinfoilIndex, new JsonSerializerOptions { WriteIndented = true });

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