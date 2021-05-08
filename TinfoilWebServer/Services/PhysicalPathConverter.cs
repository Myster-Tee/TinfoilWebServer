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
            var sanitizedRelPath = urlRelPathDecoded.TrimStart('/');

            if (string.IsNullOrEmpty(sanitizedRelPath))
            {
                isRoot = true;
                return null;
            }

            isRoot = false;

            var pathParts = sanitizedRelPath.Split('/', 2); // NOTE: whatever the string value, the length of the returned array can be 1 or 2

            var servedDirAlias = pathParts[0];
            var servedDir = _servedDirAliasMap.GetServedDir(servedDirAlias);
            if (servedDir == null)
                return null;

            string physicalPath;
            if (pathParts.Length <= 1)
                physicalPath = servedDir;
            else
                physicalPath = Path.GetFullPath(Path.Combine(servedDir, pathParts[1]));

            return physicalPath;
        }
    }
}