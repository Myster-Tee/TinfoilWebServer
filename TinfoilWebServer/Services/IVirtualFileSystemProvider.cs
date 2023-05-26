using TinfoilWebServer.Services.VirtualFS;

namespace TinfoilWebServer.Services;

public interface IVirtualFileSystemProvider
{

    /// <summary>
    /// Returns the root of the served Virtual File Systen
    /// </summary>
    VirtualFileSystemRoot Root { get; }

    public void Initialize();

}