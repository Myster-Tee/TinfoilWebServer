namespace TinfoilWebServer.Services
{
    public interface IServedDirsAliasMapper
    {
        string? GetAlias(string servedDir);

        string? GetServedDir(string alias);
    }
}