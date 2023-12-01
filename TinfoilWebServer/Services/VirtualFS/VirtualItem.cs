using System.IO;

namespace TinfoilWebServer.Services.VirtualFS;

public abstract class VirtualItem
{

    protected VirtualItem(string key, FileSystemInfo? item)
    {
        Key = key;
        Item = item;
    }

    /// <summary>
    /// The key of this item (<see cref="Item"/>)
    /// </summary>
    public string Key { get; }

    public FileSystemInfo? Item { get; }

    public VirtualDirectory? Parent { get; protected internal set; }

    public override string ToString()
    {
        return $"[{Key}]=>[{Item}]";
    }

}