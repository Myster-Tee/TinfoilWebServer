using System;
using System.Collections.Generic;
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
    private readonly ILogger<BasicAuthMiddleware> _logger;
    private static readonly Encoding _encoding = Encoding.GetEncoding("iso-8859-1");
    private readonly Dictionary<string, IAllowedUser> _allowedBase64Accounts = new();

    public BasicAuthMiddleware(IAppSettings appSettings, ILogger<BasicAuthMiddleware> logger)
    {
        if (appSettings == null)
            throw new ArgumentNullException(nameof(appSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var authenticationSettings = appSettings.Authentication;

        if (authenticationSettings != null)
        {
            foreach (var allowedUser in authenticationSettings.Users)
            {
                var bytes = _encoding.GetBytes($"{allowedUser.Name}:{allowedUser.Password}");

                var base64String = Convert.ToBase64String(bytes);
                if (!_allowedBase64Accounts.TryAdd(base64String, allowedUser)) 
                    _logger.LogWarning($"Duplicated User \"{allowedUser.Name}\" found in configuration file \"{Program.ExpectedConfigFilePath}\".");
            }
        }
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var headersAuthorization = context.Request.Headers.Authorization;

        var headerValue = headersAuthorization.FirstOrDefault();
        if (headerValue == null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.CompleteAsync();
            return;
        }

        var strings = headerValue.Split(new[] { ' ' }, 2);

        if (strings.Length != 2)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.CompleteAsync();
            return;
        }

        if (!string.Equals("Basic", strings[0], StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.CompleteAsync();
            return;
        }

        var base64IncomingAccount = strings[1];
        if (!_allowedBase64Accounts.TryGetValue(base64IncomingAccount, out var allowedUser))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.CompleteAsync();
            return;
        }

        context.User = new GenericPrincipal(new GenericIdentity(allowedUser.Name), Array.Empty<string>());

        _logger.LogInformation($"Incoming request from user \"{allowedUser.Name}\".");

        await next.Invoke(context);

    }

}