using System;
using System.IO;

namespace TinfoilWebServer.Services
{
    public class PhysicalPathConverter : IPhysicalPathConverter
    {
        private readonly IServedDirAliasMap _servedDirAliasMap;

        public PhysicalPathConverter(IServedDirAliasMap servedDirAliasMap)
        {
            _servedDirAliasMap = servedDirAliasMap ?? throw new ArgumentNullException(nameof(servedDirAliasMap));
        }

        public string? Convert(string urlRelPathDecoded, out bool isRoot)
        {
            var pathParts = SanitizeRelPath(urlRelPathDecoded).Split('/', 2);

            isRoot = false;
            if (pathParts.Length <= 1)
            {
                isRoot = true;
                return null;
            }

            var servedDirAlias = pathParts[0];
            var servedDir = _servedDirAliasMap.GetServedDir(servedDirAlias);
            if (servedDir == null)
                return null;

            var physicalPath = Path.GetFullPath(Path.Combine(servedDir, pathParts[1]));
            
            return physicalPath;
        }

        private static string SanitizeRelPath(string urlRelPathDecoded)
        {
            if (urlRelPathDecoded.StartsWith('/'))
                return urlRelPathDecoded[1..];
            return urlRelPathDecoded;
        }


    }
}