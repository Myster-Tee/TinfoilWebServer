using System;
using Microsoft.AspNetCore.Http;
using TinfoilWebServer.Services.VirtualFS;

namespace TinfoilWebServer.Services;

public class VirtualItemFinder : IVirtualItemFinder
{
    private readonly IVirtualFileSystemRootProvider _virtualFileSystemRootProvider;

    public VirtualItemFinder(IVirtualFileSystemRootProvider virtualFileSystemRootProvider)
    {
        _virtualFileSystemRootProvider = virtualFileSystemRootProvider ?? throw new ArgumentNullException(nameof(virtualFileSystemRootProvider));
    }

    public VirtualItem? Find(PathString pathString)
    {
        var path = pathString.Value;
        if (path == null)
            return null;

        var segmentHelper = new SegmentHelper(path);
        var segment = segmentHelper.GetNextSegment(out var segmentsRemaining);

        // Hack: in order to avoid having a named segment for root like «SomeAuthority/SomeRootName/SomeRootChild» (or empty named segment served as «SomeAuthority//SomeRootChild»),
        // the root is served as «SomeAuthority/» and children of root served as «SomeAuthority/SomeRootChild»
        if (segment == "" && segmentsRemaining == false)
            return _virtualFileSystemRootProvider.Root;

        VirtualDirectory dirTemp = _virtualFileSystemRootProvider.Root;

        while (true)
        {
            var childTemp = dirTemp.GetChild(segment);

            if (!segmentsRemaining)
                return childTemp;

            if (childTemp is not VirtualDirectory childDirTemp)
                return null;

            dirTemp = childDirTemp;

            segment = segmentHelper.GetNextSegment(out segmentsRemaining);
        }
    }
}

internal class SegmentHelper
{
    private string _uriPath;

    public SegmentHelper(string uriPath)
    {
        if (uriPath.StartsWith('/')) // Removes the first slash separating Authority and Path
            uriPath = uriPath[1..];

        _uriPath = uriPath;
    }

    public string GetNextSegment(out bool segmentsRemaining)
    {
        var parts = _uriPath.Split('/', 2);
        if (parts.Length > 1)
        {
            segmentsRemaining = true;
            _uriPath = parts[1];
            return parts[0];
        }
        segmentsRemaining = false;
        return parts[0];
    }
}