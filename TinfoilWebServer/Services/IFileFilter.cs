using System.Diagnostics.Contracts;

namespace TinfoilWebServer.Services;

public interface IFileFilter
{

    [Pure]
    bool IsFileAllowed(string? file);
}