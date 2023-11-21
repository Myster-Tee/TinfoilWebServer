using System.Text.Json.Nodes;

namespace TinfoilWebServer.Services;

public class CustomIndexManager : ICustomIndexManager
{
    public JsonObject? GetDefaultIndex()
    {
        //TODO: to implement

        //using var fileStream = File.Open(_appSettings.CustomIndexPath, FileMode.Open);
        //var jsonNode = JsonNode.Parse(fileStream) as JsonObject;

        throw new System.NotImplementedException();
    }

    public JsonObject? GetUserIndex(string? userName)
    {
        throw new System.NotImplementedException();
    }
}