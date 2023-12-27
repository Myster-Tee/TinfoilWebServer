using Microsoft.Extensions.Logging.Abstractions;

namespace TinfoilWebServer.Logging.Formatting.Parts;

public class StaticPart : Part
{
    public string Text { get; }

    public StaticPart(string text)
    {
        Text = text;
    }

    public override string GetText<TState>(LogEntry<TState> logEntry)
    {
        return Text;
    }
}