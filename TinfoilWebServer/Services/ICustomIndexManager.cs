using System.Text.Json.Nodes;

namespace TinfoilWebServer.Services;

/// <summary>
/// Provides all Custom Indexes which are referenced in settings
/// </summary>
public interface ICustomIndexManager
{
    /// <summary>
    /// The raw path of the custom index as written in settings files
    /// </summary>
    /// <param name="customIndexPath"></param>
    /// <returns></returns>
    JsonObject? GetCustomIndex(string? customIndexPath);

}