using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.AspNetCore.Http;

namespace TinfoilWebServer.Services.VirtualFS;

public static class VirtualItemExtension
{
    /// <summary>
    /// Builds the keys path for reaching this item
    /// </summary>
    /// <param name="virtualItem"></param>
    /// <param name="relativeParent">If not null, the returned path is made relative to this specified directory</param>
    /// <returns></returns>
    [Pure]
    public static List<string> BuildRelativeKeysPath(this VirtualItem virtualItem, VirtualDirectory? relativeParent = null)
    {
        var keysPath = new List<string>();
        var itemTemp = virtualItem;

        do
        {
            keysPath.Insert(0, itemTemp.Key);
            itemTemp = itemTemp.Parent;
        } while (itemTemp != null && itemTemp != relativeParent);

        return keysPath;
    }

    /// <summary>
    /// Build the relative URL of the specified item
    /// </summary>
    /// <param name="virtualItem"></param>
    /// <param name="relativeParent"></param>
    /// <returns></returns>
    [Pure]
    public static string BuildRelativeUrl(this VirtualItem virtualItem, VirtualDirectory? relativeParent = null)
    {
        var keysPath = virtualItem.BuildRelativeKeysPath(relativeParent);

        var buildRelativeUrl = new PathString($"/{string.Join('/', keysPath)}").ToUriComponent()[1..];

        return buildRelativeUrl;
    }
}