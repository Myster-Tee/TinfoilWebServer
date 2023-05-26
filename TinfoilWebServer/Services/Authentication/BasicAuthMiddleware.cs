using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services.Authentication;

public class BasicAuthMiddleware : IBasicAuthMiddleware
{
    private static readonly Encoding _encoding = Encoding.GetEncoding("iso-8859-1");
    private readonly HashSet<string> _allowedBase64Accounts = new();

    public BasicAuthMiddleware(IAppSettings appSettings)
    {
        if (appSettings == null)
            throw new ArgumentNullException(nameof(appSettings));

        var authenticationSettings = appSettings.Authentication;

        if (authenticationSettings != null)
        {
            foreach (var allowedUser in authenticationSettings.AllowedUsers)
            {
                var bytes = _encoding.GetBytes($"{allowedUser.Name}:{allowedUser.Password}");

                _allowedBase64Accounts.Add(Convert.ToBase64String(bytes));
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
        if (!_allowedBase64Accounts.Contains(base64IncomingAccount))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.CompleteAsync();
            return;
        }
        await next.Invoke(context);

    }

}