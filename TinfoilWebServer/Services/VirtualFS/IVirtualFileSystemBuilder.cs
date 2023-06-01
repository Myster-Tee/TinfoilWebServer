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
    VirtualFileSystemRoot BuildHierarchical(IReadOnlyList<string> servedDirectories, bool excludeEmptyDirectories);

    /// <summary>
    /// Build the served files immediately under the returned root
    /// </summary>
    /// <param name="servedDirectories"></param>
    /// <returns></returns>
    [Pure]
    VirtualFileSystemRoot BuildFlat(IReadOnlyList<string> servedDirectories);
}