namespace TinfoilWebServer.Services;

public interface IPhysicalPathConverter
{

    string? Convert(string urlRelPathDecoded, out bool isRoot);

}