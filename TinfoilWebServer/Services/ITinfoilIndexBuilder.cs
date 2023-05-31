using System.Diagnostics.Contracts;
using TinfoilWebServer.Models;
using TinfoilWebServer.Services.VirtualFS;

namespace TinfoilWebServer.Services;

public interface ITinfoilIndexBuilder
{

    /// <summary>
    /// Builds a <see cref="TinfoilIndex"/> model from the specified VirtualDirectory
    /// </summary>
    /// <param name="serverUrlRoot"></param>
    /// <param name="virtualDirectory"></param>
    /// <param name="indexType"></param>
    /// <param name="messageOfTheDay"></param>
    /// <returns></returns>
    [Pure]
    TinfoilIndex Build(string serverUrlRoot, VirtualDirectory virtualDirectory, TinfoilIndexType indexType, string? messageOfTheDay);

}

public enum TinfoilIndexType
{
    Flatten,
    Hierarchical,
}