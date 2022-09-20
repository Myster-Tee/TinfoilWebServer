namespace TinfoilWebServer.Services;

public interface IFileFilter
{

    bool IsFileAllowed(string? filePath);
}