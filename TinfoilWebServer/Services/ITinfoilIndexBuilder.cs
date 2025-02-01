using System.Diagnostics.Contracts;
using System.Text.Json.Nodes;
using TinfoilWebServer.Services.VirtualFS;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services;

public interface ITinfoilIndexBuilder
{

    /// <summary>
    /// Builds a Tinfoil index (https://blawar.github.io/tinfoil/custom_index/) from the specified VirtualDirectory
    /// </summary>
    /// <param name="virtualDirectory"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    [Pure]
    JsonObject Build(VirtualDirectory virtualDirectory, IUserInfo? user);

    /// <summary>
    /// Builds a Tinfoil index which contains only a simple message
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    [Pure]
    JsonObject BuildSimpleMessage(string? message);
}
