namespace TinfoilWebServer.Services.VirtualFS;

public class VirtualFile : VirtualItem
{

    public VirtualFile(string key, string fullLocalPath, long size) : base(key, fullLocalPath)
    {
        Size = size;
    }

    public long Size { get; }

    public override string ToString()
    {
        return $"FILE: {base.ToString()}";
    }
}


