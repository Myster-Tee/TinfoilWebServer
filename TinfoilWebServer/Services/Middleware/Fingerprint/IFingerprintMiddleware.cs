using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TinfoilWebServer.Services.Middleware.Fingerprint;

/// <summary>
/// Middleware in charge of injecting the <see cref="IFingerprintValidator"/> feature in the context
/// </summary>
public interface IFingerprintMiddleware : IMiddleware
{

}

public interface IFingerprintValidator
{
    /// <summary>
    /// The fingerprint from the current request
    /// </summary>
    string? Fingerprint { get; }

    /// <summary>
    /// Return true if fingerprint is accepted, false otherwise.
    /// When false is returned, the HTTP response is already sent.
    /// </summary>
    /// <returns></returns>
    Task<bool> Validate();
}