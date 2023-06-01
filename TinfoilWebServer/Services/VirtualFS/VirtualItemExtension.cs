using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.AspNetCore.Http;
using TinfoilWebServer.Utils;

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


    public static List<VirtualDirectory> RemoveDirectoriesWithoutFile(this VirtualDirectory directory)
    {
        var leafDirectories = directory.GetDescendantDirectories().Where(vd => vd.Directories.Length <= 0).ToList();

        var removedDirectories = new List<VirtualDirectory>();


        foreach (var leafDirectory in leafDirectories)
        {
            var dirTemp = leafDirectory;

            while (dirTemp != directory)
            {
                if (dirTemp.Files.Length > 0 || dirTemp.Directories.Length > 0)
                    break;

                // Here temp dir has no files neither directories, we remove it
                var parentDirTemp = dirTemp.Parent;

                if (parentDirTemp == null)
                    break;

                if (parentDirTemp.RemoveDirectory(dirTemp))
                    removedDirectories.Add(dirTemp);

                dirTemp = parentDirTemp;
            }

        }

        return removedDirectories;

    }

    /// <summary>
    /// Deep return the sub directories of this directory
    /// </summary>
    /// <param name="virtualDirectory"></param>
    /// <param name="includeThisDirectory">true to include this directory in the list of returned directories</param>
    /// <returns></returns>
    public static IEnumerable<VirtualDirectory> GetDescendantDirectories(this VirtualDirectory virtualDirectory, bool includeThisDirectory = false)
    {
        var remainingDirectories = new List<VirtualDirectory>();

        if (includeThisDirectory)
            remainingDirectories.Add(virtualDirectory);
        else
            remainingDirectories.AddRange(virtualDirectory.Directories);

        while (remainingDirectories.TryRemoveFirst(out var remainingDirectory))
        {
            remainingDirectories.AddRange(remainingDirectory.Directories);
            yield return remainingDirectory;
        }
    }

    /// <summary>
    /// Return all the files contains in this directory, including files from all sub-directories
    /// </summary>
    /// <param name="virtualDirectory"></param>
    /// <returns></returns>
    public static IEnumerable<VirtualFile> GetDescendantFiles(this VirtualDirectory virtualDirectory)
    {
        return virtualDirectory.GetDescendantDirectories(true).SelectMany(directory => directory.Files);
    }
}