using Microsoft.Extensions.Logging.Abstractions;
using TinfoilWebServer.Logging.Formatting.BasePartModels;
using TinfoilWebServer.Logging.Formatting.ExPartModels;
using TinfoilWebServer.Logging.Formatting.LogEntryPartModels;

namespace TinfoilWebServer.Logging.Formatting;


/// <summary>
/// Root model of the definition for the formatting of a <see cref="LogEntry{TState}"/>.
/// </summary>
public class LogEntryFormat
{

    /// <summary>
    /// The list of parts for a <see cref="LogEntry{TState}"/>.
    /// </summary>
    public LogEntryParts LogEntryParts { get; set; } = new();

    /// <summary>
    /// The list of parts for the formatting of <see cref="LogEntry{TState}.Exception"/>, for <see cref="IPart"/> of type <see cref="ExceptionLogEntryPart"/>.
    /// </summary>
    public ExParts ExParts { get; set; } = new();

    /// <summary>
    /// The default format for <see cref="LogEntry{TState}"/> without <see cref="LogEntry{TState}.Exception"/>.
    /// </summary>
    public static LogEntryFormat Default => new()
    {
        LogEntryParts = new LogEntryParts { new DateLogEntryPart(), new StaticLogEntryPart(" "), new LogLevelLogEntryPart(), new StaticLogEntryPart(": "), new MessageLogEntryPart(), new ExceptionLogEntryPart() },
        ExParts = new ExParts{ new NewLineExPart(),
            new StaticExPart("Exception Type: "), new TypeExPart(), new NewLineExPart(),
            new StaticExPart("Exception Message: "), new MessageExPart(), new NewLineExPart(),
            new StaticExPart("Exception Stack Trace: "), new StackTraceExPart()
        }
    };
}