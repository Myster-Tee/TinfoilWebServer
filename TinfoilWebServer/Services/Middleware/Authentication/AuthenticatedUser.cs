using System;
using System.Security.Claims;
using System.Security.Principal;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services.Middleware.Authentication;

public class AuthenticatedUser : ClaimsPrincipal, IUserInfo
{
    private readonly IUserInfo _userInfo;

    public AuthenticatedUser(IUserInfo userInfo)
    {
        _userInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
    }

    public override IIdentity? Identity => new GenericIdentity(_userInfo.Name);

    public string Name => _userInfo.Name;

    public string? CustomIndexPath => _userInfo.CustomIndexPath;

    public string? MessageOfTheDay => _userInfo.MessageOfTheDay;
}