using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Services.Middleware.Authentication;

namespace TinfoilWebServer.Services.Middleware.Fingerprint;

public class FingerprintMiddleware : IFingerprintMiddleware
{
    private readonly ILogger<FingerprintMiddleware> _logger;
    private readonly IFingerprintsFilteringManager _fingerprintsFilteringManager;

    public FingerprintMiddleware(IFingerprintsFilteringManager fingerprintsFilteringManager, ILogger<FingerprintMiddleware> logger)
    {
        _fingerprintsFilteringManager = fingerprintsFilteringManager ?? throw new ArgumentNullException(nameof(fingerprintsFilteringManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var incomingFingerprint = context.Request.Headers["UID"].FirstOrDefault();
        if (incomingFingerprint != null)
            _logger.LogDebug($"Request [{context.TraceIdentifier}] received with fingerprint \"{incomingFingerprint}\".");

        var authenticatedUser = context.User as AuthenticatedUser;

        context.Features.Set<IFingerprintValidator>(new FingerprintValidator(context, _fingerprintsFilteringManager, authenticatedUser, incomingFingerprint));

        await next.Invoke(context);
    }

    private class FingerprintValidator : IFingerprintValidator
    {
        private readonly HttpContext _context;
        private readonly IFingerprintsFilteringManager _fingerprintsFilteringManager;
        private readonly AuthenticatedUser? _authenticatedUser;
        private readonly string? _incomingFingerprint;

        public FingerprintValidator(HttpContext context, IFingerprintsFilteringManager fingerprintsFilteringManager, AuthenticatedUser? authenticatedUser, string? incomingFingerprint)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _fingerprintsFilteringManager = fingerprintsFilteringManager ?? throw new ArgumentNullException(nameof(fingerprintsFilteringManager));
            _authenticatedUser = authenticatedUser;
            _incomingFingerprint = incomingFingerprint;
        }

        public string? Fingerprint => _incomingFingerprint;

        public async Task<bool> Validate()
        {
            var accepted = _fingerprintsFilteringManager.AcceptFingerprint(_incomingFingerprint, _authenticatedUser?.UserInfo, _context.TraceIdentifier);
            if (accepted)
                return true;

            _context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await _context.Response.CompleteAsync();

            return false;
        }
    }
}