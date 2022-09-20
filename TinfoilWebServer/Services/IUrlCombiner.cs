using System;

namespace TinfoilWebServer.Services;

public interface IUrlCombiner
{
    public Uri BaseAbsUrl { get; }

    public Uri CombineLocalPath(string localRelPath);
}