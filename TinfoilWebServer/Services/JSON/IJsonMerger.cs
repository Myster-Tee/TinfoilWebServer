using System.Text.Json.Nodes;

namespace TinfoilWebServer.Services.JSON;

public interface IJsonMerger
{
    public JsonObject Merge(JsonObject baseObj, params JsonObject?[] others);
}