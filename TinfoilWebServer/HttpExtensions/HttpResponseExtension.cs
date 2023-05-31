using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace TinfoilWebServer.HttpExtensions;

public static class HttpResponseExtension
{
    public static async Task WriteFile(this HttpResponse response, string filePath, string contentType = "application/octet-stream", RangeHeaderValue? rangeHeader = null)
    {
        RangeItemHeaderValue? range = null;
        if (rangeHeader is { Ranges.Count: > 0 })
            range = rangeHeader.Ranges.First();

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