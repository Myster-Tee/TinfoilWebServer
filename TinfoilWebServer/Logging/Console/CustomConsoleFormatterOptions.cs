using Microsoft.Extensions.Logging.Console;

namespace TinfoilWebServer.Logging.Console;

/// <summary>
/// Model for console format options
/// </summary>
public class CustomConsoleFormatterOptions : ConsoleFormatterOptions
{

    public string? Format { get; set; }

    public string? FormatWithException { get; set; }

    public bool UseColor { get; set; } = true;

}