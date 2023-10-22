using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using TinfoilWebServer.Booting;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services.Middleware.Authentication;

public class BasicAuthMiddleware : IBasicAuthMiddleware
{
    private readonly IAuthenticationSettings _authenticationSettings;
    private readonly ILogger<BasicAuthMiddleware> _logger;
    private readonly IBootInfo _bootInfo;
    private static readonly Encoding _encoding = Encoding.GetEncoding("iso-8859-1");
    private readonly Dictionary<string, IAllowedUser> _allowedBase64Accounts = new();

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
                _logger.LogInformation($"Web browser authentication enabled.");
            else
                _logger.LogInformation($"Web browser authentication disabled.");
        }
    }

    private void LoadAllowedUsers(bool isReload)
    {
        _allowedBase64Accounts.Clear();

        foreach (var allowedUser in _authenticationSettings.Users)
        {
            var bytes = _encoding.GetBytes($"{allowedUser.Name}:{allowedUser.Password}");

            var base64String = Convert.ToBase64String(bytes);
            if (!_allowedBase64Accounts.TryAdd(base64String, allowedUser))
                _logger.LogWarning($"Duplicated user \"{allowedUser.Name}\" found in configuration file \"{_bootInfo.ConfigFileFullPath}\".");
        }

        _logger.LogInformation($"List of allowed users successfully {(isReload ? "reloaded" : "loaded")}, {_allowedBase64Accounts.Count} user(s) found (authentication is {(_authenticationSettings.Enabled ? "enabled" : "disabled")}).");
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
            _logger.LogDebug($"Incoming request \"{context.TraceIdentifier}\" is missing authentication header.");
            await RespondUnauthorized(context);
            return;
        }

        var strings = headerValue.Split(new[] { ' ' }, 2);

        if (strings.Length != 2)
        {
            _logger.LogDebug($"Incoming request \"{context.TraceIdentifier}\" authorization header invalid, space separator missing.");
            await RespondUnauthorized(context);
            return;
        }

        if (!string.Equals("Basic", strings[0], StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug($"Incoming request \"{context.TraceIdentifier}\" authentication header is not basic, found \"{strings[0]}\".");
            await RespondUnauthorized(context);
            return;
        }

        var base64IncomingAccount = strings[1];
        if (!_allowedBase64Accounts.TryGetValue(base64IncomingAccount, out var allowedUser))
        {
            _logger.LogDebug($"Incoming request \"{context.TraceIdentifier}\" login or password incorrect.");
            await RespondUnauthorized(context);
            return;
        }

        context.User = new AuthenticatedUser(allowedUser);

        _logger.LogInformation($"Incoming request \"{context.TraceIdentifier}\" from user \"{allowedUser.Name}\".");

        await next.Invoke(context);

    }

    private async Task RespondUnauthorized(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

        if (_authenticationSettings.WebBrowserAuthEnabled)
            context.Response.Headers.WWWAuthenticate = new StringValues("Basic");

        await context.Response.CompleteAsync();
    }
}