using Microsoft.Extensions.Logging.Abstractions;

namespace TinfoilWebServer.Logging.Formatting.Parts;

public class MessagePart : Part
{
    public override string GetText<TState>(LogEntry<TState> logEntry)
    {
        return logEntry.Formatter(logEntry.State, logEntry.Exception);
    }
}