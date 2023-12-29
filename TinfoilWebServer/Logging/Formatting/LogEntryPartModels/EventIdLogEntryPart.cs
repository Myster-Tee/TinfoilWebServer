using Microsoft.Extensions.Logging.Abstractions;

namespace TinfoilWebServer.Logging.Formatting.LogEntryPartModels;

public class EventIdLogEntryPart : ILogEntryPart
{
    public string GetText<TState>(LogEntry<TState> logEntry)
    {
        return logEntry.EventId.Id.ToString();
    }
}