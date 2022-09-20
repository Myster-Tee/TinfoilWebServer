namespace TinfoilWebServer.Services;

public interface IJsonSerializer
{
    string Serialize(object obj);
}