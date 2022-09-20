using System.Text.Encodings.Web;
using System.Text.Json;

namespace TinfoilWebServer.Services;

public class JsonSerializer : IJsonSerializer
{
    public string Serialize(object obj)
    {
        return System.Text.Json.JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // NOTE: required to avoid escaping of some special chars like '+', '&', etc. (See https://docs.microsoft.com/fr-fr/dotnet/standard/serialization/system-text-json-character-encoding for more information)
        });
    }
}