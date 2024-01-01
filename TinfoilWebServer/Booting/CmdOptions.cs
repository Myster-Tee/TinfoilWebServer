using CommandLine;

namespace TinfoilWebServer.Booting;

/// <summary>
/// Model of command line options
/// </summary>
public class CmdOptions
{
    [Option('c', "config", Required = false, HelpText = "Custom location of the configuration file.")]
    public string? ConfigFilePath { get; set; }

    [Option('d', "currentDir", Required = false, HelpText = "Change the current directory.")]
    public string? CurrentDirectory { get; set; }

    [Option('s', "winService", Required = false, HelpText = "Run the server as a Windows service.")]
    public bool RunAsWindowsService { get; set; } = false;

    [Option("sha256", Required = false, HelpText = "Compute SHA256 passwords interactively.")]
    public bool ComputeSha256Passwords { get; set; } = false;

}