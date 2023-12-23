using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NReco.Logging.File;

namespace TinfoilWebServer.Logging.Formatting;

public class LogFileFormatter
{
    private readonly IConfigurationSection _loggingConfig;
    private string? _lastLogFormat = null;
    private LogFormatter _formatter = LogFormatter.Default;


    public LogFileFormatter(IConfigurationSection loggingConfig)
    {
        _loggingConfig = loggingConfig ?? throw new ArgumentNullException(nameof(loggingConfig));
    }

    public string FormatLogEntry(LogMessage message)
    {
        var logFormat = _loggingConfig.GetValue<string>("LogFormat");
        if (!ReferenceEquals(logFormat, _lastLogFormat))
        {
            _formatter = string.IsNullOrEmpty(logFormat) ? LogFormatter.Default : LogFormatter.Parse(logFormat);
            _lastLogFormat = logFormat;
        }

        return _formatter.Format(new LogEntry<string>(message.LogLevel, message.LogName, message.EventId, message.Message, message.Exception, (s, _) => s));
    }

}