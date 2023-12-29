using Microsoft.Extensions.Logging.Abstractions;
using TinfoilWebServer.Logging.Formatting.BasePartModels;

namespace TinfoilWebServer.Logging.Formatting.LogEntryPartModels;

public class DateLogEntryPart : DateBasePart, ILogEntryPart
{
    public string GetText<TState>(LogEntry<TState> logEntry)
    {
        return base.GetDateString();
    }
}