using Microsoft.Extensions.Logging.Abstractions;
using TinfoilWebServer.Logging.Formatting.BasePartModels;

namespace TinfoilWebServer.Logging.Formatting.LogEntryPartModels;

public interface ILogEntryPart : IPart
{
    public string? GetText<TState>(LogEntry<TState> logEntry);
}