using System.IO;
using System.Text.Json;

namespace TinfoilWebServer.Services.Middleware.Fingerprint;

public class FingerprintsSerializer : IFingerprintsSerializer
{
    public void Serialize(FileInfo file, AllowedFingerprints allowedFingerprints)
    {
        using var fileStream = file.OpenWrite();
        JsonSerializer.Serialize(fileStream, allowedFingerprints, new JsonSerializerOptions { WriteIndented = true });
    }

    public AllowedFingerprints Deserialize(FileInfo file)
    {
        using var fileStream = file.OpenRead();
        var allowedFingerprints = JsonSerializer.Deserialize<AllowedFingerprints>(fileStream);
        if (allowedFingerprints == null)
            throw new FileLoadException("JSON null.");

        return allowedFingerprints;
    }
}