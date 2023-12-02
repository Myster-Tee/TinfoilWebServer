namespace TinfoilWebServer.Services;

/// <summary>
/// Service in charge of auto refreshing the served files (calling <see cref="IVirtualFileSystemRootProvider.Refresh"/>) when a change is detected in the served directories
/// </summary>
public interface IVFSAutoRefreshManager
{
    /// <summary>
    /// Initialize the service
    /// </summary>
    void Initialize();
}