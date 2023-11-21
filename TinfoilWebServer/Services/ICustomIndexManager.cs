using System.Text.Json.Nodes;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services;

public interface ICustomIndexManager
{
    /// <summary>
    /// Provides the JSON object corresponding to <see cref="IAppSettings.CustomIndexPath"/> if defined.
    /// </summary>
    /// <returns></returns>
    JsonObject? GetDefaultIndex();

    /// <summary>
    /// Provides the JSON object corresponding to <see cref="IAllowedUser.CustomIndexPath"/> if defined.
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    JsonObject? GetUserIndex(string? userName);

}