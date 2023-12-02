using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Security.Principal;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services.Middleware.Authentication;

/// <summary>
/// Middleware in charge of injecting <see cref="AuthenticatedUser"/> in <see cref="HttpContext.User"/> property when authentication is enable and user is successfully authenticated.
/// </summary>
public interface IBasicAuthMiddleware : IMiddleware
{
}

public class AuthenticatedUser : ClaimsPrincipal
{
    public AuthenticatedUser(IUserInfo userInfo)
    {
        UserInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
        Identity = new GenericIdentity(userInfo.Name, "Basic");
    }

    public override IIdentity Identity { get; }

    public IUserInfo UserInfo { get; }

}

