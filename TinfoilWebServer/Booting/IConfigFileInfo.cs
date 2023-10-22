namespace TinfoilWebServer.Booting;

public interface IBootInfo
{
    string ConfigFileFullPath { get; }

    CmdOptions CmdOptions { get; }
}

public class BootInfo : IBootInfo
{
    public string ConfigFileFullPath { get; init; } = null!;

    public CmdOptions CmdOptions { get; init; } = null!;
}