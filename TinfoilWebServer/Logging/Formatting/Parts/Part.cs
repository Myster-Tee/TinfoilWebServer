using Microsoft.Extensions.Logging.Abstractions;

namespace TinfoilWebServer.Logging.Formatting.Parts;

/// <summary>
/// A part is a piece of final logged message composition.
/// </summary>
public abstract class Part
{
    public abstract string? GetText<TState>(LogEntry<TState> logEntry);
}