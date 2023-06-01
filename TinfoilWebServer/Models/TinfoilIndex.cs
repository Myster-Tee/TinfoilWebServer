using System;
using System.Text.Json.Serialization;

namespace TinfoilWebServer.Models;

/// <summary>
/// Tinfoil HTTP documentation
/// https://blawar.github.io/tinfoil/network/
/// https://blawar.github.io/tinfoil/custom_index/
/// </summary>
public class TinfoilIndex
{

    /// <summary>
    /// "files": ["https://url1", "sdmc:/url2", "http://url3"],
    /// </summary>
    [JsonPropertyName("files")]
    public FileNfo[] Files { get; set; } = Array.Empty<FileNfo>();

    /// <summary>
    /// "directories": ["https://url1", "sdmc:/url2", "http://url3"],
    /// </summary>
    [JsonPropertyName("directories")]
    public string[] Directories { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Message of the Day
    /// </summary>
    [JsonPropertyName("success")]
    public string? Success { get; set; }
}

public class FileNfo
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }

}