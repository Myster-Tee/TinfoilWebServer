using System.Diagnostics.Contracts;
using System.Text.Json.Nodes;
using TinfoilWebServer.Services.VirtualFS;

namespace TinfoilWebServer.Services;

public interface ITinfoilIndexBuilder
{
    /// <summary>
    /// Builds a Tinfoil index (https://blawar.github.io/tinfoil/custom_index/) from the specified VirtualDirectory
    /// </summary>
    /// <param name="virtualDirectory"></param>
    /// <param name="userName"></param>
    /// <returns></returns>
    [Pure]
    JsonObject Build(VirtualDirectory virtualDirectory, string? userName);

}
