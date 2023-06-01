using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace TinfoilWebServer.Services.VirtualFS;

public interface IVirtualFileSystemBuilder
{
    /// <summary>
    /// Build the served files tree
    /// </summary>
    /// <param name="servedDirectories"></param>
    /// <param name="excludeEmptyDirectories"></param>
    /// <returns></returns>
    [Pure]
    VirtualFileSystemRoot Build(IReadOnlyList<string> servedDirectories, bool excludeEmptyDirectories);
}