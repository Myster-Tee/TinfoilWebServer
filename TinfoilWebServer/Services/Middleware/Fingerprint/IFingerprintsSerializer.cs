using System.Collections.Generic;
using System.IO;

namespace TinfoilWebServer.Services.Middleware.Fingerprint;

public interface IFingerprintsSerializer
{
    void Serialize(FileInfo file, AllowedFingerprints allowedFingerprints);

    AllowedFingerprints Deserialize(FileInfo file);
}

/// <summary>
/// Model for serialization
/// </summary>
public class AllowedFingerprints
{
    public List<string> Global { get; set; } = new();

    public Dictionary<string, List<string>> PerUser { get; set; } = new();
}