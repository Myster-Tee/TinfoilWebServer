using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TinfoilWebServer.Logging.Formatting.LogEntryPartModels;

public class LogLevelLogEntryPart : ILogEntryPart
{
    public LogLevelMode Mode { get; set; } = LogLevelMode.ShortUpper;

    public string GetText<TState>(LogEntry<TState> logEntry)
    {
        var level = logEntry.LogLevel;
        switch (Mode)
        {
            case LogLevelMode.Upper:
                return level.ToString().ToUpper();
            case LogLevelMode.Lower:
                return level.ToString().ToLower();
            case LogLevelMode.ShortUpper:
                return GetShortUpper(level).ToUpper();
            case LogLevelMode.ShortLower:
                return GetShortUpper(level).ToLower();
            case LogLevelMode.Normal:
            default:
                return level.ToString();
        }
    }

    private string GetShortUpper(LogLevel logLevel)
    {
        switch (logLevel)
        {
            case LogLevel.Trace:
                return "[T]";
            case LogLevel.Debug:
                return "[D]";
            case LogLevel.Information:
                return "[I]";
            case LogLevel.Warning:
                return "[W]";
            case LogLevel.Error:
                return "[E]";
            case LogLevel.Critical:
                return "[C]";
            default:
                return "[U]"; // Unknown
        }
    }
}

public enum LogLevelMode
{
    Normal,
    Upper,
    Lower,
    ShortUpper,
    ShortLower,
}