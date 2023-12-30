using Microsoft.Extensions.Logging.Abstractions;

namespace TinfoilWebServer.Logging.Formatting.LogEntryPartModels;

public class CategoryLogEntryPart : ILogEntryPart
{
    public string GetText<TState>(LogEntry<TState> logEntry)
    {
        return logEntry.Category;
    }
}