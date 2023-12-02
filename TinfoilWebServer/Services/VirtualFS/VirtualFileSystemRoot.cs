namespace TinfoilWebServer.Services.VirtualFS;

/// <summary>
/// Represents the root of the virtual file system.
/// The goal of the virtual file system is to isolate served files from real file system files.
/// </summary>
public class VirtualFileSystemRoot : VirtualDirectory
{

    public VirtualFileSystemRoot() : base("", null)
    {
    }

}
