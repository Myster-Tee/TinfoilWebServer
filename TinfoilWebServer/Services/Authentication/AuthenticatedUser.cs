using System;
using System.Security.Claims;
using System.Security.Principal;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services.Authentication;

public class AuthenticatedUser : ClaimsPrincipal
{
    public AuthenticatedUser(IAllowedUser allowedUser)
    {
        AllowedUser = allowedUser ?? throw new ArgumentNullException(nameof(allowedUser));
    }

    public override IIdentity? Identity => new GenericIdentity(AllowedUser.Name);

    public IAllowedUser AllowedUser { get; }
}