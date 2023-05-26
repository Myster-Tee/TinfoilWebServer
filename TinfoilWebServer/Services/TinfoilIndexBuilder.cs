using System;
using System.Linq;
using TinfoilWebServer.Models;
using TinfoilWebServer.Services.VirtualFS;

namespace TinfoilWebServer.Services;

public class TinfoilIndexBuilder : ITinfoilIndexBuilder
{
    public TinfoilIndex Build(string serverUrlRoot, VirtualDirectory virtualDirectory, TinfoilIndexType indexType, string? messageOfTheDay)
    {
        var tinfoilIndex = new TinfoilIndex
        {
            Success = messageOfTheDay
        };
        switch (indexType)
        {
            case TinfoilIndexType.Hierarchical:
                tinfoilIndex.Directories.AddRange(virtualDirectory.Directories.Select(vd => serverUrlRoot + vd.RelativeUri));
                tinfoilIndex.Files.AddRange(virtualDirectory.Files.Select(vf => new FileNfo { Size = vf.Size, Url = serverUrlRoot + vf.RelativeUri }));
                break;
            case TinfoilIndexType.Flatten:
                tinfoilIndex.Files.AddRange(virtualDirectory.GetDescendantFiles().Select(vf => new FileNfo { Size = vf.Size, Url = serverUrlRoot + vf.RelativeUri }));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(indexType), indexType, null);
        }

        return tinfoilIndex;
    }

}