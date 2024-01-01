using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using TinfoilWebServer.Booting;
using TinfoilWebServer.Settings;
using TinfoilWebServer.Utils;

namespace TinfoilWebServer.Services.Middleware.Authentication;

public class BasicAuthMiddleware : IBasicAuthMiddleware
{
    private readonly IAuthenticationSettings _authenticationSettings;
    private readonly ILogger<BasicAuthMiddleware> _logger;
    private readonly IBootInfo _bootInfo;
    private readonly Dictionary<string, IAllowedUser> _allowedUsersPerName = new();

    public BasicAuthMiddleware(IAuthenticationSettings authenticationSettings, ILogger<BasicAuthMiddleware> logger, IBootInfo bootInfo)
    {

        _authenticationSettings = authenticationSettings ?? throw new ArgumentNullException(nameof(authenticationSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _bootInfo = bootInfo ?? throw new ArgumentNullException(nameof(bootInfo));

        _authenticationSettings.PropertyChanged += OnAuthenticationSettingsChanged;

        LoadAllowedUsers(false);
    }

    private void OnAuthenticationSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAuthenticationSettings.Users))
        {
            LoadAllowedUsers(true);
        }
        else if (e.PropertyName == nameof(IAuthenticationSettings.Enabled))
        {
            if (_authenticationSettings.Enabled)
                _logger.LogInformation($"Authentication enabled.");
            else
                _logger.LogWarning($"Authentication disabled.");
        }
        else if (e.PropertyName == nameof(IAuthenticationSettings.WebBrowserAuthEnabled))
        {
            if (_authenticationSettings.WebBrowserAuthEnabled)
                _logger.LogInformation($"Web Browser authentication enabled.");
            else
                _logger.LogInformation($"Web Browser authentication disabled.");
        }
        else if (e.PropertyName == nameof(IAuthenticationSettings.PwdType))
        {
            _logger.LogInformation($"Password type changed to {_authenticationSettings.PwdType}.");
        }
    }

    private void LoadAllowedUsers(bool isReload)
    {
        _allowedUsersPerName.Clear();

        foreach (var allowedUser in _authenticationSettings.Users)
        {
            if (allowedUser.Name.Contains(':'))
            {
                _logger.LogWarning($"Invalid configuration file \"{_bootInfo.ConfigFileFullPath}\": user name \"{allowedUser.Name}\" can't contain colon (not allowed in Basic Authentication).");
                continue;
            }

            if (!_allowedUsersPerName.TryAdd(allowedUser.Name, allowedUser))
                _logger.LogWarning($"Invalid configuration file \"{_bootInfo.ConfigFileFullPath}\": user \"{allowedUser.Name}\" duplicated.");
        }

        _logger.LogInformation($"List of allowed users successfully {(isReload ? "reloaded" : "loaded")}, {_allowedUsersPerName.Count} user(s) found (authentication is {(_authenticationSettings.Enabled ? "enabled" : "disabled")}).");
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!_authenticationSettings.Enabled)
        {
            await next.Invoke(context);
            return;
        }

        var headersAuthorization = context.Request.Headers.Authorization;

        var headerValue = headersAuthorization.FirstOrDefault();
        if (headerValue == null)
        {
            _logger.LogWarning($"Request [{context.TraceIdentifier}] is missing authentication header.");
            await RespondUnauthorized(context, true);
            return;
        }

        if (!TryParseBasicAuthHeaderValue(headerValue, context.TraceIdentifier, out var incomingUserName, out var incomingPassword))
        {
            await RespondUnauthorized(context);
            return;
        }

        if (!_allowedUsersPerName.TryGetValue(incomingUserName, out var allowedUser))
        {
            _logger.LogWarning($"Request [{context.TraceIdentifier}] rejected, user \"{incomingUserName}\" not found.");
            await RespondUnauthorized(context);
            return;
        }

        bool pwdAllowed;
        switch (_authenticationSettings.PwdType)
        {
            case PwdType.Plaintext:
                pwdAllowed = string.Equals(incomingPassword, allowedUser.Password);
                break;
            case PwdType.Sha256:
                var incomingPwdHash = HashHelper.ComputeSha256(incomingPassword);
                pwdAllowed = string.Equals(incomingPwdHash, allowedUser.Password, StringComparison.OrdinalIgnoreCase);
                break;
            default:
                _logger.LogError($"Request [{context.TraceIdentifier}] rejected for user \"{incomingUserName}\": password type \"{_authenticationSettings.PwdType}\" not supported!");
                await RespondUnauthorized(context);
                return;
        }

        if (!pwdAllowed)
        {
            _logger.LogWarning($"Request [{context.TraceIdentifier}] rejected for user \"{incomingUserName}\": password incorrect.");
            await RespondUnauthorized(context);
        }

        _logger.LogDebug($"Request [{context.TraceIdentifier}] passed authentication for user user \"{allowedUser.Name}\".");

        context.User = new AuthenticatedUser(allowedUser);

        await next.Invoke(context);
    }


    private bool TryParseBasicAuthHeaderValue(string headerValue, string traceId, [NotNullWhen(true)] out string? userName, [NotNullWhen(true)] out string? password)
    {
        var strings = headerValue.Split(new[] { ' ' }, 2);

        if (strings.Length != 2)
        {
            _logger.LogWarning($"Request [{traceId}] authorization header invalid, space separator missing.");
            userName = null;
            password = null;
            return false;
        }

        if (!string.Equals("Basic", strings[0], StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning($"Request [{traceId}] authentication header is not basic, found \"{strings[0]}\".");
            userName = null;
            password = null;
            return false;
        }

        var base64IncomingAccount = strings[1];

        var bytes = new Span<byte>(new byte[base64IncomingAccount.Length]); // NOTE: Base64 string length is always longer than the number of decoded bytes
        if (!Convert.TryFromBase64String(base64IncomingAccount, bytes, out var nbBytesWritten))
        {
            _logger.LogWarning($"Request [{traceId}] authentication header is not basic, found \"{strings[0]}\".");
            userName = null;
            password = null;
            return false;
        }

        var decodedString = Encoding.UTF8.GetString(bytes.Slice(0, nbBytesWritten));
        var parts = decodedString.Split(':', 2);
        if (parts.Length != 2)
        {
            _logger.LogWarning($"Request [{traceId}] authentication header invalid, colon separator missing in decoded base64 string \"{decodedString}\".");
            userName = null;
            password = null;
            return false;
        }

        userName = parts[0];
        password = parts[1];

        return true;
    }

    private async Task RespondUnauthorized(HttpContext context, bool basicHeaderMissing = false)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

        if (_authenticationSettings.WebBrowserAuthEnabled && basicHeaderMissing)
            context.Response.Headers.WWWAuthenticate = new StringValues("Basic");

        await context.Response.CompleteAsync();
    }
}