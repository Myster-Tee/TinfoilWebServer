using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace TinfoilWebServer.Services.VirtualFS;

public class VirtualDirectory : VirtualItem
{
    public DirectoryInfo? Directory { get; }

    private readonly Dictionary<string, VirtualItem> _childItemsByKey = new();

    public VirtualFile[] Files => _childItemsByKey.Values.OfType<VirtualFile>().ToArray();

    public VirtualDirectory[] Directories => _childItemsByKey.Values.OfType<VirtualDirectory>().ToArray();

    public VirtualDirectory(string key, DirectoryInfo? directory) : base(key, directory)
    {
        Directory = directory;
    }

    public void AddDirectory(VirtualDirectory virtualDirectory)
    {
        if (virtualDirectory == null)
            throw new ArgumentNullException(nameof(virtualDirectory));

        virtualDirectory.Parent?.RemoveDirectory(virtualDirectory);

        _childItemsByKey.Add(virtualDirectory.Key, virtualDirectory);
        virtualDirectory.Parent = this;
    }

    public bool RemoveDirectory(VirtualDirectory virtualDirectory)
    {
        if (virtualDirectory == null)
            throw new ArgumentNullException(nameof(virtualDirectory));

        if (!_childItemsByKey.TryGetValue(virtualDirectory.Key, out var foundItem) || foundItem != virtualDirectory)
            return false;

        virtualDirectory.Parent = null;
        return _childItemsByKey.Remove(virtualDirectory.Key);

    }

    public void AddFile(VirtualFile virtualFile)
    {
        if (virtualFile == null)
            throw new ArgumentNullException(nameof(virtualFile));

        _childItemsByKey.Add(virtualFile.Key, virtualFile);
        virtualFile.Parent = this;
    }

    [Pure]
    public VirtualItem? GetChild(string key)
    {
        _childItemsByKey.TryGetValue(key, out var result);
        return result;
    }

    public bool ChildExists(string key)
    {
        return _childItemsByKey.ContainsKey(key);
    }

    public override string ToString()
    {
        return $"DIRECTORY: {base.ToString()}";
    }


}
