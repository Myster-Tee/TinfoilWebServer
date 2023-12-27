using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NReco.Logging.File;
using TinfoilWebServer.Logging.Formatting;

namespace TinfoilWebServer.Logging.File;

public class LogFileFormatter
{
    private LogEntryFormat _format = LogEntryFormat.Default;
    private LogEntryFormat _formatWithException = LogEntryFormat.DefaultWithException;


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
        var format = options.Format;
        _format = string.IsNullOrEmpty(format) ? LogEntryFormat.Default : LogEntryFormat.Parse(format);

        var formatWithException = options.FormatWithException;
        _formatWithException = string.IsNullOrEmpty(formatWithException) ? LogEntryFormat.DefaultWithException : LogEntryFormat.Parse(formatWithException);
    }

    public string FormatLogEntry(LogMessage message)
    {
        var logEntry = new LogEntry<string>(message.LogLevel, message.LogName, message.EventId, message.Message, message.Exception, (s, _) => s);

        var logText = logEntry.Exception == null
            ? logEntry.Format(_format)
            : logEntry.Format(_formatWithException);

        return logText;
    }
}