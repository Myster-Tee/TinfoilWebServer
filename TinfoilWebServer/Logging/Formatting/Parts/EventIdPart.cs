using Microsoft.Extensions.Logging.Abstractions;

namespace TinfoilWebServer.Logging.Formatting.Parts;

public class EventIdPart : Part
{
    public override string GetText<TState>(LogEntry<TState> logEntry)
    {
        return logEntry.EventId.Id.ToString();
    }
}