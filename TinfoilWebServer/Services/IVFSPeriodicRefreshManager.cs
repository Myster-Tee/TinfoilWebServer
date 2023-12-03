namespace TinfoilWebServer.Services;

/// <summary>
/// Service in charge of auto refreshing the served files (calling <see cref="IVirtualFileSystemRootProvider.SafeRefresh"/>) once a delay is reached
/// </summary>
public interface IVFSPeriodicRefreshManager
{
    /// <summary>
    /// Initialize the service
    /// </summary>
    void Initialize();

}