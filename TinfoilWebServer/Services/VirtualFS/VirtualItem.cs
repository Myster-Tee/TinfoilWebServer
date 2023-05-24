namespace TinfoilWebServer.Services.VirtualFS;

public abstract class VirtualItem
{

    protected VirtualItem(string fullLocalPath)
    {
        FullLocalPath = fullLocalPath;
    }

    public abstract string UriSegment { get; }

    public string FullLocalPath { get; }

    public VirtualDirectory? Parent { get; protected internal set; }

    public override string ToString()
    {
        return $"[{UriSegment}]=>[{FullLocalPath}]";
    }

    public string RelativeUri
    {
        get
        {
            var itemTemp = this;
            var path = "";
            do
            {
                path = itemTemp.UriSegment + path;
                itemTemp = itemTemp.Parent;

            } while (itemTemp != null);

            return path;
        }
    }

}