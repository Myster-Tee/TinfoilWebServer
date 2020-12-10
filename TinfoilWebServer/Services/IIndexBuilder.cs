using System;
using TinfoilWebServer.Models;

namespace TinfoilWebServer.Services
{
    public interface IIndexBuilder
    {
        TinfoilIndex Build(string directory, Uri correspondingUri, string? messageOfTheDay);
    }
}