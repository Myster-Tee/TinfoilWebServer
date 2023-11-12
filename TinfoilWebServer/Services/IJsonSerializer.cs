using System.Diagnostics.Contracts;
using System.Text.Json.Nodes;

namespace TinfoilWebServer.Services;

public interface IJsonSerializer
{
    [Pure]
    string Serialize(JsonObject obj);
}