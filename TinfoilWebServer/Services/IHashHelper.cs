namespace TinfoilWebServer.Services;

public interface IHashHelper
{
    string ComputeSha256(string text);
}