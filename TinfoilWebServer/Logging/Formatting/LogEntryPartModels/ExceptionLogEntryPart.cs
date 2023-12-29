using Microsoft.Extensions.Logging.Abstractions;

namespace TinfoilWebServer.Logging.Formatting.LogEntryPartModels;

public class ExceptionLogEntryPart : ILogEntryPart
{
    public string? GetText<TState>(LogEntry<TState> logEntry)
    {
        return null;
    }
}