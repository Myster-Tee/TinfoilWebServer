using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NReco.Logging.File;
using TinfoilWebServer.Logging.Formatting;

namespace TinfoilWebServer.Logging.File;

public class LogFileFormatter
{
    private LogEntryFormat _logEntryFormat = LogEntryFormat.Default;


    /// <summary>
    /// Unfortunately the way NReco.Logging is implemented doesn't allow to use DI constructor with <see cref="IOptionsMonitor{TOptions}"/>.
    /// This is why this method exists.
    /// </summary>
    /// <param name="optionsMonitor"></param>
    public void Initialize(IOptionsMonitor<FileFormatterOptions> optionsMonitor)
    {
        optionsMonitor.OnChange(InitFromOptions);
        InitFromOptions(optionsMonitor.CurrentValue);
    }

    private void InitFromOptions(FileFormatterOptions options)
    {
        _logEntryFormat = LogEntryFormat.Default;

        var format = options.Format;
        if (!string.IsNullOrEmpty(format))
            _logEntryFormat.LogEntryParts = LogEntryParts.Parse(format);

        var exceptionFormat = options.ExceptionFormat;

        if (!string.IsNullOrEmpty(exceptionFormat))
            _logEntryFormat.ExParts = ExParts.ParseException(exceptionFormat);
    }

    public string FormatLogEntry(LogMessage message)
    {
        var logEntry = new LogEntry<string>(message.LogLevel, message.LogName, message.EventId, message.Message, message.Exception, (s, _) => s);

        var logText = logEntry.Format(_logEntryFormat);

        return logText;
    }
}