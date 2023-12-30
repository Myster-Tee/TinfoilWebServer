using Microsoft.Extensions.Logging.Abstractions;

namespace TinfoilWebServer.Logging.Formatting.LogEntryPartModels;

public class MessageLogEntryPart : ILogEntryPart
{
    public string GetText<TState>(LogEntry<TState> logEntry)
    {
        return logEntry.Formatter(logEntry.State, logEntry.Exception);
    }
}