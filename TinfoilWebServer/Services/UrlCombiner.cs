using System;
using System.IO;
using System.Web;


namespace TinfoilWebServer.Services;

public class UrlCombiner : IUrlCombiner
{
    public UrlCombiner(Uri baseAbsUrl)
    {
        if (baseAbsUrl == null) 
            throw new ArgumentNullException(nameof(baseAbsUrl));

        if (!baseAbsUrl.IsAbsoluteUri)
            throw new ArgumentException("Uri should be absolute", nameof(baseAbsUrl));

        BaseAbsUrl = SanitizeBaseUrl(baseAbsUrl);
    }

    public Uri BaseAbsUrl { get; }

    public Uri CombineLocalPath(string localRelPath)
    {
        var localPathWithSlashes = localRelPath.Replace(Path.DirectorySeparatorChar, '/').TrimStart('/');

        var relPathEncoded = HttpUtility.UrlPathEncode(localPathWithSlashes);

        var newUri = new Uri(BaseAbsUrl, new Uri(relPathEncoded, UriKind.Relative));

        return newUri;
    }

    /// <summary>
    /// Ensures that the returned URI ends with a slash to ensure combination
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private static Uri SanitizeBaseUrl(Uri url)
    {
        var rooDirUri = url.OriginalString.EndsWith('/') ? url : new Uri(url.OriginalString + "/");
        return rooDirUri;
    }
}