using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TinfoilWebServer.Models
{
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
        public List<FileNfo> Files { get; set; } = new List<FileNfo>();

        /// <summary>
        /// "directories": ["https://url1", "sdmc:/url2", "http://url3"],
        /// </summary>
        [JsonPropertyName("directories")]
        public List<string> Directories { get; set; } = new List<string>();

        /// <summary>
        /// Message of the Day
        /// </summary>
        [JsonPropertyName("success")]
        public string? Success { get; set; }
    }

    public class FileNfo
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

    }

}
