using CommandLine;

namespace TinfoilWebServer.Booting;

/// <summary>
/// Model of command line options
/// </summary>
public class CmdOptions
{
    [Option('c', "config", Required = false, HelpText = "Custom location of the configuration file.")]
    public string? ConfigFilePath { get; set; }


    [Option('w', "workingDir", Required = false, HelpText = "Change the working directory.")]
    public string? WorkingDirectory { get; set; }
}