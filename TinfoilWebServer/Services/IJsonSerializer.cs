using System.Diagnostics.Contracts;

namespace TinfoilWebServer.Services;

public interface IJsonSerializer
{
    [Pure]
    string Serialize(object obj);
}