using Microsoft.Extensions.Logging.Abstractions;

namespace TinfoilWebServer.Logging.Formatting.Parts;

public class ExMessagePart : Part
{
    public override string? GetText<TState>(LogEntry<TState> logEntry)
    {
        return logEntry.Exception?.Message;
    }
}