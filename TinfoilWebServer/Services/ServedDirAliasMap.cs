using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services;

public class ServedDirAliasMap : IServedDirAliasMap
{
    private readonly Dictionary<string, string> _servedDirPerAlias = new();
    private readonly object _lock = new();

    public ServedDirAliasMap(IAppSettings appSettings)
    {
        foreach (var servedDirectory in appSettings.ServedDirectories)
        {
            var dirNameBase = Path.GetFileName(servedDirectory)!;

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
        lock (_lock)
        {
            foreach (var (alias, servedDirTmp) in _servedDirPerAlias)
            {
                if (servedDirTmp.Equals(servedDir))
                    return alias;
            }

            return null;
        }
    }

    public string? GetServedDir(string alias)
    {
        lock (_lock)
        {
            _servedDirPerAlias.TryGetValue(alias, out var servedDir);
            return servedDir;
        }
    }

    public IEnumerator<DirWithAlias> GetEnumerator()
    {
        lock (_lock)
        {
            return _servedDirPerAlias.Select(pair => new DirWithAlias
            {
                Alias = pair.Key,
                DirPath = pair.Value
            }).ToList().GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

}