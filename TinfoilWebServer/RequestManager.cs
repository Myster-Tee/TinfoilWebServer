using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using ElMariachi.Http.Header.Managed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
        private readonly IFilesStructureBuilder _filesStructureBuilder;
        private readonly IFileFilter _fileFilter;

        public RequestManager(IAppSettings appSettings, IWebHostEnvironment webHostEnvironment, IFilesStructureBuilder filesStructureBuilder, IFileFilter fileFilter)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _filesStructureBuilder = filesStructureBuilder ?? throw new ArgumentNullException(nameof(filesStructureBuilder));
            _fileFilter = fileFilter ?? throw new ArgumentNullException(nameof(fileFilter));
        }

        public async Task OnRequest(HttpContext context)
        {
            var requestPath = context.Request.Path;
            if (string.Equals(requestPath, "/favicon.ico", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 200;
                await context.Response.Body.WriteAsync(Resources.Favicon);
                return;
            }

            var decodedPath = HttpUtility.UrlDecode(requestPath);
            var requestedPath = _webHostEnvironment.ContentRootFileProvider.GetFileInfo(decodedPath).PhysicalPath;
            var request = context.Request;

            if (Directory.Exists(requestedPath) && request.Method == "GET" || request.Method == "HEAD")
            {
                var url = $"{context.Request.Scheme}://{context.Request.Host}{requestPath}";
                var uri = new Uri(url);

                var mainPayload = _filesStructureBuilder.Build(requestedPath, uri);

                var json = JsonSerializer.Serialize(mainPayload, new JsonSerializerOptions { WriteIndented = true });

                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsync(json, Encoding.UTF8);
            }
            else if (_fileFilter.IsFileAllowed(requestedPath) && File.Exists(requestedPath) && request.Method == "GET" || request.Method == "HEAD")
            {
                var rangeHeader = new RangeHeader
                {
                    RawValue = context.Request.Headers["range"]
                };

                var ranges = rangeHeader.Ranges;
                var range = ranges.Count == 1 ? ranges[0] : null;

                await context.Response.WriteFile(requestedPath, range: range);
            }
            else
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("<!DOCTYPE html><http><head><title>Oops!</title></head><body style='text-align:center'>404<br>Not found...</body></html>");
            }

        }

    }
}