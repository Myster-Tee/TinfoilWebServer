namespace TinfoilWebServer.Services.VirtualFS;

public abstract class VirtualItem
{

    protected VirtualItem(string key, string fullLocalPath)
    {
        Key = key;
        FullLocalPath = fullLocalPath;
    }

    /// <summary>
    /// The key of this item (<see cref="FullLocalPath"/>)
    /// </summary>
    public string Key { get; }

    public string FullLocalPath { get; }

    public VirtualDirectory? Parent { get; protected internal set; }

    public override string ToString()
    {
        return $"[{Key}]=>[{FullLocalPath}]";
    }

}