namespace TinfoilWebServer.Services
{
    public interface IServedDirAliasMapper
    {
        string? GetAlias(string servedDir);

        string? GetServedDir(string alias);
    }
}