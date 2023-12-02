using System.IO;

namespace TinfoilWebServer.Services.VirtualFS;

public class VirtualFile : VirtualItem
{

    public VirtualFile(string key, FileInfo file, long size) : base(key, file)
    {
        File = file;
        Size = size;
    }

    public FileInfo File { get; }

    public long Size { get; }

    public override string ToString()
    {
        return $"FILE: {base.ToString()}";
    }
}


