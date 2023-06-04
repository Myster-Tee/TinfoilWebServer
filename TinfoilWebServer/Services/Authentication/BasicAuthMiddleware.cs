using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services.Authentication;

public class BasicAuthMiddleware : IBasicAuthMiddleware
{
    private readonly IAuthenticationSettings _authenticationSettings;
    private readonly ILogger<BasicAuthMiddleware> _logger;
    private static readonly Encoding _encoding = Encoding.GetEncoding("iso-8859-1");
    private readonly Dictionary<string, IAllowedUser> _allowedBase64Accounts = new();

    public BasicAuthMiddleware(IAuthenticationSettings authenticationSettings, ILogger<BasicAuthMiddleware> logger)
    {

        _authenticationSettings = authenticationSettings ?? throw new ArgumentNullException(nameof(authenticationSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _authenticationSettings.PropertyChanged += OnAuthenticationSettingsChanged;

        LoadAllowedUsers();
    }

    private void OnAuthenticationSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAuthenticationSettings.Users))
        {
            LoadAllowedUsers();
        }
        else if (e.PropertyName == nameof(IAuthenticationSettings.Enabled))
        {
            if (_authenticationSettings.Enabled)
                _logger.LogInformation($"Authentication enabled.");
            else
                _logger.LogWarning($"Authentication disabled.");
        }

    }

    private void LoadAllowedUsers()
    {
        _allowedBase64Accounts.Clear();

        foreach (var allowedUser in _authenticationSettings.Users)
        {
            var bytes = _encoding.GetBytes($"{allowedUser.Name}:{allowedUser.Password}");

            var base64String = Convert.ToBase64String(bytes);
            if (!_allowedBase64Accounts.TryAdd(base64String, allowedUser))
                _logger.LogWarning($"Duplicated user \"{allowedUser.Name}\" found in configuration file \"{Program.ExpectedConfigFilePath}\".");
        }

        _logger.LogInformation($"List of allowed users successfully loaded, {_allowedBase64Accounts.Count} user(s) found.");
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
            _logger.LogDebug("Incoming request is missing authentication header.");
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.CompleteAsync();
            return;
        }

        var strings = headerValue.Split(new[] { ' ' }, 2);

        if (strings.Length != 2)
        {
            _logger.LogDebug("Authorization header invalid, space separator missing.");
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.CompleteAsync();
            return;
        }

        if (!string.Equals("Basic", strings[0], StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug($"Authentication is not basic, found \"{strings[0]}\".");
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.CompleteAsync();
            return;
        }

        var base64IncomingAccount = strings[1];
        if (!_allowedBase64Accounts.TryGetValue(base64IncomingAccount, out var allowedUser))
        {
            _logger.LogDebug($"Login or password incorrect.");
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.CompleteAsync();
            return;
        }

        // Retrieves the UID of the Mariko account sent by Tinfoil
        var uid = context.Request.Headers["UID"].ToString();

        var allowedUids = allowedUser.UIDs;
        if (allowedUids != null && allowedUids.Length > 0)
        {
            if (!allowedUids.Contains(uid))
            {
                _logger.LogDebug($"UID \"{uid}\" not accepted.");
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.CompleteAsync();
                return;
            }
        }

        context.User = new GenericPrincipal(new GenericIdentity(allowedUser.Name), Array.Empty<string>());

        _logger.LogInformation($"Incoming request from user \"{allowedUser.Name}\" with UID \"{uid}\".");

        await next.Invoke(context);

    }

}