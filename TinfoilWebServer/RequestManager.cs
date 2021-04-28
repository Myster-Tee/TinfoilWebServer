using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using ElMariachi.Http.Header.Managed;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ITinfoilIndexBuilder _tinfoilIndexBuilder;
        private readonly IFileFilter _fileFilter;

        public RequestManager(IAppSettings appSettings, IWebHostEnvironment webHostEnvironment, ITinfoilIndexBuilder tinfoilIndexBuilder, IFileFilter fileFilter)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _tinfoilIndexBuilder = tinfoilIndexBuilder ?? throw new ArgumentNullException(nameof(tinfoilIndexBuilder));
            _fileFilter = fileFilter ?? throw new ArgumentNullException(nameof(fileFilter));
        }

        public async Task OnRequest(HttpContext context)
        {
            var request = context.Request;
            var requestPath = request.Path;
            if (string.Equals(requestPath, "/favicon.ico", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 200;
                await context.Response.Body.WriteAsync(Resources.Favicon);
                return;
            }

            //TODO
            var currentUrl = new Uri(context.Request.GetDisplayUrl());

            var decodedPath = HttpUtility.UrlDecode(requestPath);

            var physicalPath = _webHostEnvironment.ContentRootFileProvider.GetFileInfo(decodedPath).PhysicalPath;

            if (Directory.Exists(physicalPath) && request.Method == "GET" || request.Method == "HEAD")
            {
                var url = $"{request.Scheme}://{request.Host}{requestPath}";
                var uri = new Uri(url);

                var isRootPath = string.Equals(requestPath, "/") || true;
                var mainPayload = _tinfoilIndexBuilder.Build(physicalPath, uri, _appSettings.IndexType, isRootPath ? _appSettings.MessageOfTheDay : null);

                var json = JsonSerializer.Serialize(mainPayload, new JsonSerializerOptions { WriteIndented = true });

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

                await context.Response.WriteFile(physicalPath, range: range);
            }
            else
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("<!DOCTYPE html><http><head><title>Oops!</title></head><body style='text-align:center'>404<br>Not found...</body></html>");
            }

        }

    }
}