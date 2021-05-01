using System;
using System.Collections.Generic;
using TinfoilWebServer.Models;

namespace TinfoilWebServer.Services
{
    public interface ITinfoilIndexBuilder
    {
        TinfoilIndex Build(IEnumerable<Dir> dirs, TinfoilIndexType indexType, string? messageOfTheDay);
    }

    public class Dir
    {
        /// <summary>
        /// The path to the directory to use for building the <see cref="TinfoilIndex"/>
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The absolute URL corresponding the directory pointed by <see cref="Path"/>
        /// </summary>
        public Uri CorrespondingUrl { get; set; }
    }
}