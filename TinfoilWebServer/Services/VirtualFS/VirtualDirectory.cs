using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace TinfoilWebServer.Services.VirtualFS;

public class VirtualDirectory : VirtualItem
{
    private readonly Dictionary<string, VirtualItem> _childItemsByUriSegment = new();


    public VirtualFile[] Files => _childItemsByUriSegment.Values.OfType<VirtualFile>().ToArray();

    public VirtualDirectory[] Directories => _childItemsByUriSegment.Values.OfType<VirtualDirectory>().ToArray();

    public VirtualDirectory(string key, string fullLocalPath) : base(key, fullLocalPath)
    {
    }

    public void AddDirectory(VirtualDirectory virtualDirectory)
    {
        if (virtualDirectory == null)
            throw new ArgumentNullException(nameof(virtualDirectory));

        _childItemsByUriSegment.Add(virtualDirectory.Key, virtualDirectory);
        virtualDirectory.Parent = this;
    }

    public void AddFile(VirtualFile virtualFile)
    {
        if (virtualFile == null)
            throw new ArgumentNullException(nameof(virtualFile));

        _childItemsByUriSegment.Add(virtualFile.Key, virtualFile);
        virtualFile.Parent = this;
    }

    [Pure]
    public VirtualItem? GetChild(string key)
    {
        _childItemsByUriSegment.TryGetValue(key, out var result);
        return result;
    }

    public bool ChildExists(string key)
    {
        return _childItemsByUriSegment.ContainsKey(key);
    }

    public override string ToString()
    {
        return $"DIRECTORY: {base.ToString()}";
    }

    /// <summary>
    /// Deep return the sub directories of this directory
    /// </summary>
    /// <param name="includeThisDirectory">true to include this directory in the list of returned directories</param>
    /// <returns></returns>
    public IEnumerable<VirtualDirectory> GetDescendantDirectories(bool includeThisDirectory = false)
    {
        var remainingDirectories = new List<VirtualDirectory>();

        if (includeThisDirectory)
            remainingDirectories.Add(this);
        else
            remainingDirectories.AddRange(this.Directories);

        while (remainingDirectories.Count > 0)
        {
            var directory = remainingDirectories[0];
            remainingDirectories.RemoveAt(0);
            remainingDirectories.AddRange(directory.Directories);
            yield return directory;
        }
    }

    /// <summary>
    /// Return all the files contains in this directory, including files from all sub-directories
    /// </summary>
    /// <returns></returns>
    public IEnumerable<VirtualFile> GetDescendantFiles()
    {
        return GetDescendantDirectories(true).SelectMany(directory => directory.Files);
    }
}
