using Microsoft.Extensions.Logging.Abstractions;

namespace TinfoilWebServer.Logging.Formatting.Parts;

public class CategoryPart : Part
{
    public override string GetText<TState>(LogEntry<TState> logEntry)
    {
        return logEntry.Category;
    }
}