using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services.Middleware.Fingerprint;

public interface IFingerprintsFilteringManager
{

    bool AcceptFingerprint(string? fingerprint, IUserInfo? userInfo, string traceId);

    /// <summary>
    /// Initializes manager
    /// </summary>
    void Initialize();
}