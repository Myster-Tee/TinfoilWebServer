using Microsoft.AspNetCore.Http;

namespace TinfoilWebServer.Services.VirtualFS;

public static class VirtualItemExtension
{

    public static string ToEscapedUrl(this VirtualItem virtualItem, string urlRoot)
    {
        var itemTemp = virtualItem;

        var path = $"";
        do
        {
            path = $"/{itemTemp.Key}{path}";
            itemTemp = itemTemp.Parent;
        } while (itemTemp != null && itemTemp is not VirtualFileSystemRoot);


        return urlRoot + new PathString(path).ToUriComponent();
    }

}