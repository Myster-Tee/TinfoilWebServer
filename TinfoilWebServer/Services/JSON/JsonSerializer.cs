using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TinfoilWebServer.Services.JSON;

public class JsonSerializer : IJsonSerializer
{
    public string Serialize(JsonObject obj)
    {
        var json = obj.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // NOTE: required to avoid escaping of some special chars like '+', '&', etc. (See https://docs.microsoft.com/fr-fr/dotnet/standard/serialization/system-text-json-character-encoding for more information)
        });

        return json;
    }
}