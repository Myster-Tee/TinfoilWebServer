using TinfoilWebServer.Services.VirtualFS;

namespace TinfoilWebServer.Services;

public interface IVirtualFileSystemRootProvider
{

    /// <summary>
    /// Returns the root of the served Virtual File Systen
    /// </summary>
    VirtualFileSystemRoot Root { get; }

    /// <summary>
    /// Initializes cache
    /// </summary>
    void Initialize();

}