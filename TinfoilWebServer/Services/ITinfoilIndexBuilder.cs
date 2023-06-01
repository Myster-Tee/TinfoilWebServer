using System.Diagnostics.Contracts;
using TinfoilWebServer.Models;
using TinfoilWebServer.Services.VirtualFS;

namespace TinfoilWebServer.Services;

public interface ITinfoilIndexBuilder
{

    /// <summary>
    /// Builds a <see cref="TinfoilIndex"/> model from the specified VirtualDirectory
    /// </summary>
    /// <param name="virtualDirectory"></param>
    /// <returns></returns>
    [Pure]
    TinfoilIndex Build(VirtualDirectory virtualDirectory);

}
