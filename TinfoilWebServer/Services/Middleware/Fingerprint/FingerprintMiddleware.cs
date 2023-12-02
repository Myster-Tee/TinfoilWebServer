using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Services.Middleware.Authentication;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services.Middleware.Fingerprint;

public class FingerprintMiddleware : IFingerprintMiddleware
{
    private readonly IDevicesFilteringSettings _devicesFilteringSettings;
    private readonly IAuthenticationSettings _authenticationSettings;
    private readonly ILogger<FingerprintMiddleware> _logger;

    public FingerprintMiddleware(IDevicesFilteringSettings devicesFilteringSettings, IAuthenticationSettings authenticationSettings, ILogger<FingerprintMiddleware> logger)
    {
        _devicesFilteringSettings = devicesFilteringSettings ?? throw new ArgumentNullException(nameof(devicesFilteringSettings));
        _authenticationSettings = authenticationSettings ?? throw new ArgumentNullException(nameof(authenticationSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _devicesFilteringSettings.PropertyChanged += OnDevicesFilteringSettingsChanged;
        _authenticationSettings.PropertyChanged += OnAuthenticationSettingsChanged;

        CheckSettingsConsistency();
    }

    private void OnAuthenticationSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAuthenticationSettings.WebBrowserAuthEnabled))
        {
            CheckSettingsConsistency();
        }
        else if (e.PropertyName == nameof(IAuthenticationSettings.Enabled))
        {
            CheckSettingsConsistency();
        }
    }

    private void OnDevicesFilteringSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IDevicesFilteringSettings.AllowedFingerprints))
        {
            if (_devicesFilteringSettings.AllowedFingerprints.Count > 0)
                _logger.LogInformation("Global fingerprints filtering enabled.");
            else
                _logger.LogInformation("Global fingerprints filtering disabled.");
            CheckSettingsConsistency();
        }
    }


    private void CheckSettingsConsistency()
    {
        if (_devicesFilteringSettings.AllowedFingerprints.Count > 0 && _authenticationSettings is { Enabled: true, WebBrowserAuthEnabled: true })
        {
            _logger.LogWarning($"Inconsistent configuration: Web Browser authentication is enabled ({nameof(IAuthenticationSettings.WebBrowserAuthEnabled)}) " +
                               $"as well as fingerprints filtering ({nameof(IDevicesFilteringSettings.AllowedFingerprints)}), " +
                               $"but Web Browsers never send fingerprints, only Tinfoil does.");
        }
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var incomingFingerprint = context.Request.Headers["UID"].FirstOrDefault();

        if (incomingFingerprint != null)
            _logger.LogInformation($"Incoming request \"{context.TraceIdentifier}\" received with fingerprint \"{incomingFingerprint}\".");

        Action? logAction;
        bool isAccepted;

        // Priority to fingerprints of authenticated user if defined
        var authenticatedUser = context.User as AuthenticatedUser;
        var userFingerprintsToCheck = authenticatedUser?.UserInfo.AllowedFingerprints;
        if (userFingerprintsToCheck is { Count: > 0 })
        {
            // Fingerprints of current authenticated user are enabled

            if (incomingFingerprint == null)
            {
                isAccepted = false;
                logAction = () => _logger.LogWarning($"Incoming request \"{context.TraceIdentifier}\" from authenticated user \"{authenticatedUser!.UserInfo.Name}\" received without fingerprint, request rejected.");
            }
            else if (!userFingerprintsToCheck.Contains(incomingFingerprint))
            {
                isAccepted = false;
                logAction = () => _logger.LogWarning($"Incoming request \"{context.TraceIdentifier}\" from authenticated user \"{authenticatedUser!.UserInfo.Name}\" received with invalid fingerprint \"{incomingFingerprint}\", request rejected.");
            }
            else
            {
                isAccepted = true;
                logAction = null;
            }
        }
        else
        {
            var globalFingerprintsToCheck = _devicesFilteringSettings.AllowedFingerprints;
            if (globalFingerprintsToCheck.Count > 0)
            {
                // Global fingerprints are enabled

                if (incomingFingerprint == null)
                {
                    isAccepted = false;
                    logAction = () => _logger.LogWarning($"Incoming request \"{context.TraceIdentifier}\" received without fingerprint, request rejected.");
                }
                else if (!globalFingerprintsToCheck.Contains(incomingFingerprint))
                {
                    isAccepted = false;
                    logAction = () => _logger.LogWarning($"Incoming request \"{context.TraceIdentifier}\" received with invalid fingerprint \"{incomingFingerprint}\", request rejected.");
                }
                else
                {
                    isAccepted = true;
                    logAction = null;
                }
            }
            else
            {
                // No declared fingerprint to control from configuration

                isAccepted = true;
                logAction = null;
            }
        }

        context.Features.Set<IFingerprintValidator>(new FingerprintValidator(isAccepted, logAction));

        await next.Invoke(context);
    }

    private class FingerprintValidator : IFingerprintValidator
    {
        private readonly bool _isAccepted;
        private readonly Action? _logAction;

        public FingerprintValidator(bool isAccepted, Action? logAction)
        {
            _isAccepted = isAccepted;
            _logAction = logAction;
        }

        async Task<bool> IFingerprintValidator.Validate(HttpResponse response)
        {
            _logAction?.Invoke();

            if (_isAccepted)
                return true;

            response.StatusCode = (int)HttpStatusCode.Unauthorized;

            await response.CompleteAsync();

            return false;
        }
    }
}