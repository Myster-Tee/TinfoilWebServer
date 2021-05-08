using System;

namespace TinfoilWebServer.Services
{
    public interface IUrlCombinerFactory
    {
        IUrlCombiner Create(Uri baseAbsUrl);
    }
}