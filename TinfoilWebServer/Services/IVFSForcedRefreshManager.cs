namespace TinfoilWebServer.Services;

/// <summary>
/// Service in charge of auto refreshing the served files (calling <see cref="IVirtualFileSystemRootProvider.Refresh"/>) once a delay is reached
/// </summary>
public interface IVFSForcedRefreshManager
{
    /// <summary>
    /// Initialize the service
    /// </summary>
    void Initialize();

}