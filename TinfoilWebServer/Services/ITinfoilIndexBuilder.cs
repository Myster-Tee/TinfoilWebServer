using System;
using System.Collections.Generic;
using TinfoilWebServer.Models;
using TinfoilWebServer.Services.VirtualFS;

namespace TinfoilWebServer.Services;

public interface ITinfoilIndexBuilder
{
    /// <summary>
    /// Build the JSON Tinfoil index model
    /// </summary>
    /// <param name="dirs"></param>
    /// <param name="indexType"></param>
    /// <param name="messageOfTheDay"></param>
    /// <returns></returns>
    TinfoilIndex Build(IEnumerable<Dir> dirs, TinfoilIndexType indexType, string? messageOfTheDay);


    TinfoilIndex Build(VirtualDirectory virtualDirectory, TinfoilIndexType indexType, string? messageOfTheDay);

}

public class Dir
{
    /// <summary>
    /// The path to the directory to use for building the <see cref="TinfoilIndex"/>
    /// </summary>
    public string Path { get; init; } = null!;

    /// <summary>
    /// The absolute URL corresponding the directory pointed by <see cref="Path"/>
    /// </summary>
    public Uri CorrespondingUrl { get; init; } = null!;
}