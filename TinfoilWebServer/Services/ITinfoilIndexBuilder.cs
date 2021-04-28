using System;
using TinfoilWebServer.Models;

namespace TinfoilWebServer.Services
{
    public interface ITinfoilIndexBuilder
    {
        TinfoilIndex Build(string directory, Uri correspondingUri, TinfoilIndexType indexType, string? messageOfTheDay);
    }
}