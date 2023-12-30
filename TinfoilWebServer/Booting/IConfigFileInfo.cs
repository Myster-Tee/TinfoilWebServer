using System.Collections.Generic;

namespace TinfoilWebServer.Booting;

public interface IBootInfo
{
    /// <summary>
    /// Get the full path to the configuration file
    /// </summary>
    string ConfigFileFullPath { get; }

    /// <summary>
    /// Get the program command options
    /// </summary>
    CmdOptions CmdOptions { get; }

    /// <summary>
    /// Get the list of boot errors
    /// </summary>
    List<string> Errors { get; }
}

public class BootInfo : IBootInfo
{
    public string ConfigFileFullPath { get; set; } = null!;

    public CmdOptions CmdOptions { get; init; } = null!;

    public List<string> Errors { get; } = new();
}