using System.Net;
using System.Threading.Tasks;
using ElMariachi.Http.Header.Managed;
using Microsoft.AspNetCore.Http;

namespace TinfoilWebServer.HttpExtensions;

public static class HttpResponseExt
{
    public static async Task WriteFile(this HttpResponse response, string filePath, string contentType = "application/octet-stream", IRange? range = null)
    {
        var fileSender = new FileSender(response, filePath, contentType, range);

        try
        {
            response.StatusCode = fileSender.IsPartialContent ? (int)HttpStatusCode.PartialContent : (int)HttpStatusCode.OK;
            await fileSender.Send();
        }
        finally
        {
            await fileSender.DisposeAsync();
        }

    }
}