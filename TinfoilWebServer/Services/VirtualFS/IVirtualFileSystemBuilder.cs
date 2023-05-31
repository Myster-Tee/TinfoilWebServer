using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace TinfoilWebServer.Services.VirtualFS;

public interface IVirtualFileSystemBuilder
{
    /// <summary>
    /// Build the served files tree
    /// </summary>
    /// <param name="servedDirectories"></param>
    /// <returns></returns>
    [Pure]
    VirtualFileSystemRoot BuildHierarchical(IReadOnlyList<string> servedDirectories);
}