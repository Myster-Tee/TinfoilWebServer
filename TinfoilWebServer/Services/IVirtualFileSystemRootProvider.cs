using System.Threading.Tasks;
using TinfoilWebServer.Services.VirtualFS;

namespace TinfoilWebServer.Services;

/// <summary>
/// Provides the memory cache of served files to ensure isolation of served files from the real file system
/// </summary>
public interface IVirtualFileSystemRootProvider
{

    /// <summary>
    /// Returns the root of the served Virtual File System
    /// </summary>
    VirtualFileSystemRoot Root { get; }

    /// <summary>
    /// Refresh the served files cache from served directories
    /// </summary>
    Task SafeRefresh();

}