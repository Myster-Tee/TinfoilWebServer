namespace TinfoilWebServer.Services.VirtualFS;

public interface IVirtualFileSystemBuilder
{
    /// <summary>
    /// Build the served files tree
    /// </summary>
    /// <param name="servedDirectories"></param>
    /// <returns></returns>
    VirtualFileSystemRoot BuildHierarchical(string[] servedDirectories);
}