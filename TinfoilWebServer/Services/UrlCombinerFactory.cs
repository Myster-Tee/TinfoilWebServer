using System;

namespace TinfoilWebServer.Services
{
    public class UrlCombinerFactory : IUrlCombinerFactory
    {
        public IUrlCombiner Create(Uri baseAbsUrl)
        {
            return new UrlCombiner(baseAbsUrl);
        }
    }
}