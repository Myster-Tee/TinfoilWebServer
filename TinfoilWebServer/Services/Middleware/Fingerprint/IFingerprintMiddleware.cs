using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TinfoilWebServer.Services.Middleware.Fingerprint;

public interface IFingerprintMiddleware : IMiddleware
{

}

public interface IFingerprintValidator
{
    /// <summary>
    /// Return true if fingerprint is accepted, false otherwise.
    /// When false is returned, the HTTP response is sent.
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    Task<bool> Validate(HttpResponse response);
}