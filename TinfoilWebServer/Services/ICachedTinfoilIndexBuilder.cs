using System;
using System.Collections.Generic;
using TinfoilWebServer.Models;

namespace TinfoilWebServer.Services;

public interface ICachedTinfoilIndexBuilder
{

    TinfoilIndex Build(string url, IEnumerable<Dir> dirs, TinfoilIndexType indexType, string? messageOfTheDay);

    TimeSpan CacheExpiration { get; }

}