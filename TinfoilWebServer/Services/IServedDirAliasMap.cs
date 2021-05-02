using System.Collections.Generic;

namespace TinfoilWebServer.Services
{
    public interface IServedDirAliasMap : IEnumerable<DirWithAlias>
    {
        string? GetAlias(string servedDir);

        string? GetServedDir(string alias);
    }

    public class DirWithAlias
    {
        public string DirPath { get; set; }

        public string Alias { get; set; }
    }
}