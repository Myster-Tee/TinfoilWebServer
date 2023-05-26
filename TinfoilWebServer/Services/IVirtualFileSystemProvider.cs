using TinfoilWebServer.Services.VirtualFS;

namespace TinfoilWebServer.Services;

public interface IVirtualFileSystemProvider
{

    VirtualFileSystemRoot Root { get; }

    public void Initialize();

}