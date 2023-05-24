using System;
using System.Diagnostics.Contracts;

namespace TinfoilWebServer.Services.VirtualFS;

/// <summary>
/// Represents the root of the virtual file system
/// </summary>
public class VirtualFileSystemRoot : VirtualDirectory
{

    public VirtualFileSystemRoot() : base(new DirectoryUriSegment(""), "")
    {
    }

    /// <summary>
    /// Find the <see cref="VirtualItem"/> corresponding to the given url
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    [Pure]
    public VirtualItem? ReachItem(string url)
    {
        var uri = new Uri(url);

        if (uri.Segments.Length <= 0 || uri.Segments[0] != this.UriSegment)
            return null;

        return this.ReachItem(uri.Segments[1..]);
    }

}
