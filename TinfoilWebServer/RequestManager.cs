using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Properties;
using TinfoilWebServer.Services;
using TinfoilWebServer.Services.JSON;
using TinfoilWebServer.Services.Middleware.Authentication;
using TinfoilWebServer.Services.Middleware.Fingerprint;
using TinfoilWebServer.Services.VirtualFS;
using TinfoilWebServer.Settings;
using TinfoilWebServer.Utils;

namespace TinfoilWebServer;

public class RequestManager : IRequestManager
{
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ITinfoilIndexBuilder _tinfoilIndexBuilder;
    private readonly IVirtualItemFinder _virtualItemFinder;
    private readonly IAuthenticationSettings _authenticationSettings;
    private readonly ILogger<RequestManager> _logger;


    public RequestManager(IJsonSerializer jsonSerializer, ITinfoilIndexBuilder tinfoilIndexBuilder, IVirtualItemFinder virtualItemFinder, IAuthenticationSettings authenticationSettings, ILogger<RequestManager> logger)
    {
        _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        _tinfoilIndexBuilder = tinfoilIndexBuilder ?? throw new ArgumentNullException(nameof(tinfoilIndexBuilder));
        _virtualItemFinder = virtualItemFinder ?? throw new ArgumentNullException(nameof(virtualItemFinder));
        _authenticationSettings = authenticationSettings ?? throw new ArgumentNullException(nameof(authenticationSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task OnRequest(HttpContext context)
    {
        var fingerprintValidator = context.Features.Get<IFingerprintValidator>();
        if (fingerprintValidator == null)
            throw new InvalidOperationException($"Internal error: {nameof(IFingerprintValidator)} feature can't be null!");

        var authenticatedUser = context.User as AuthenticatedUser; // Can be null when authentication is disabled
        if (_authenticationSettings.Enabled && authenticatedUser == null)
            throw new InvalidOperationException($"Internal error: when authentication is enabled, {nameof(AuthenticatedUser)} can't be null!");

        var logAuthInfo = BuildLogAuthInfo(authenticatedUser?.UserInfo.Name, fingerprintValidator.Fingerprint);

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

            // NOTE: Tinfoil's documentation states that user fingerprint (UID header) is only sent when Tinfoil requests a directory, thus the strategy can't be easily implemented in the middleware
            if (!await fingerprintValidator.Validate())
                return;

            _logger.LogInformation($"Request [{context.TraceIdentifier}] to index \"{(string.IsNullOrEmpty(virtualDirectory.Key) ? "ROOT" : virtualDirectory.Key)}\" allowed {logAuthInfo}.");

            var tinfoilIndex = _tinfoilIndexBuilder.Build(virtualDirectory, authenticatedUser?.UserInfo);

            var json = _jsonSerializer.Serialize(tinfoilIndex);

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(json, Encoding.UTF8);
        }
        else if ((request.Method is "GET" or "HEAD") && virtualItem is VirtualFile virtualFile)
        {
            _logger.LogInformation($"Request [{context.TraceIdentifier}] to file \"{virtualFile.Key}\" allowed {logAuthInfo}.");

            var rangeHeader = request.GetTypedHeaders().Range;
            context.Response.ContentType = "application/octet-stream";

            await context.Response.WriteFile(virtualFile.File.FullName, context.RequestAborted, rangeHeader: rangeHeader);
        }
        else
        {
            _logger.LogInformation($"Request [{context.TraceIdentifier}] doesn't target any valid element {logAuthInfo}.");

            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            await context.Response.CompleteAsync();
        }
    }

    private static string BuildLogAuthInfo(string? userName, string? fingerprint)
    {
        var shortFingerprint = fingerprint != null ? $"{fingerprint.Truncate(8)}..." : "N/A";

        return $"(UserName={userName ?? "N/A"}, Fingerprint={shortFingerprint})";
    }
}