using System.Collections.Generic;
using System.IO;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services
{
    public class ServedDirAliasMapper : IServedDirAliasMapper

    {
        private readonly Dictionary<string, string> _servedDirPerAlias = new();

        public ServedDirAliasMapper(IAppSettings appSettings)
        {
            foreach (var servedDirectory in appSettings.ServedDirectories)
            {
                var dirNameBase = Path.GetDirectoryName(servedDirectory)!;

                var num = 1;
                var alias = dirNameBase;
                while (_servedDirPerAlias.ContainsKey(alias))
                {
                    alias = $"{dirNameBase}_{num++}";
                }

                _servedDirPerAlias.Add(alias, servedDirectory);
            }
        }

        public string? GetAlias(string servedDir)
        {
            foreach (var (alias, servedDirTmp) in _servedDirPerAlias)
            {
                if (servedDirTmp.Equals(servedDir))
                    return alias;
            }

            return null;
        }

        public string? GetServedDir(string alias)
        {
            _servedDirPerAlias.TryGetValue(alias, out var servedDir);
            return servedDir;
        }

    }
}
