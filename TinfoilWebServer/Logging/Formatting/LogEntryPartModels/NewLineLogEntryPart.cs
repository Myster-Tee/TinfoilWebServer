using Microsoft.Extensions.Logging.Abstractions;
using TinfoilWebServer.Logging.Formatting.BasePartModels;

namespace TinfoilWebServer.Logging.Formatting.LogEntryPartModels;

public class NewLineLogEntryPart : NewLineBasePart, ILogEntryPart
{
    public  string GetText<TState>(LogEntry<TState> logEntry)
    {
        return base.NewLine;
    }
}