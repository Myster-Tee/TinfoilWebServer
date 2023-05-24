using System;

namespace TinfoilWebServer.Services.VirtualFS;

public class VirtualFile : VirtualItem
{

    public VirtualFile(FileUriSegment fileUriSegment, string fullLocalPath) : base(fullLocalPath)
    {
        UriSegment = (fileUriSegment ?? throw new ArgumentNullException(nameof(fileUriSegment))).UriSegment;
    }

    public override string UriSegment { get; }

    public override string ToString()
    {
        return $"FILE: {base.ToString()}";
    }
}


public class FileUriSegment
{
    public FileUriSegment(string fileName)
    {
        UriSegment = Uri.EscapeDataString(fileName);
    }

    public string UriSegment { get; }
}