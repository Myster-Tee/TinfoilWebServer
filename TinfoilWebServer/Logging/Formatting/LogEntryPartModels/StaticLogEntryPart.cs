using Microsoft.Extensions.Logging.Abstractions;

namespace TinfoilWebServer.Logging.Formatting.LogEntryPartModels;

public class StaticLogEntryPart : ILogEntryPart
{
    public string Text { get; }

    public StaticLogEntryPart(string text)
    {
        Text = text;
    }

    public string GetText<TState>(LogEntry<TState> logEntry)
    {
        return Text;
    }
}