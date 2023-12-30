using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using TinfoilWebServer.Logging.Formatting;
using TinfoilWebServer.Logging.Formatting.LogEntryPartModels;


namespace TinfoilWebServer.Logging.Console;

public class CustomConsoleFormatter : ConsoleFormatter
{
    private LogEntryFormat _logEntryFormat;
    private bool _useColor = true;

    public CustomConsoleFormatter(IOptionsMonitor<CustomConsoleFormatterOptions> optionsMonitor) : base(nameof(CustomConsoleFormatter))
    {
        UpdateFromOptions(optionsMonitor.CurrentValue);
        optionsMonitor.OnChange(UpdateFromOptions);
    }

    [MemberNotNull(nameof(_logEntryFormat))]
    private void UpdateFromOptions(CustomConsoleFormatterOptions options)
    {
        _logEntryFormat = LogEntryFormat.Default;

        var format = options.Format;
        if (!string.IsNullOrEmpty(format))
            _logEntryFormat.LogEntryParts = LogEntryParts.Parse(format);

        var exceptionFormat = options.ExceptionFormat;

        if (!string.IsNullOrEmpty(exceptionFormat))
            _logEntryFormat.ExParts = ExParts.ParseException(exceptionFormat);

        _useColor = options.UseColor;
    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {

        foreach (var (text, part) in logEntry.FormatParts(_logEntryFormat))
        {
            if (_useColor && part is LogLevelLogEntryPart && text != null)
            {
                var consoleColor = logEntry.LogLevel switch
                {
                    LogLevel.Trace => ConsoleColor.Magenta,
                    LogLevel.Debug => ConsoleColor.Magenta,
                    LogLevel.Information => ConsoleColor.Green,
                    LogLevel.Warning => ConsoleColor.Yellow,
                    LogLevel.Error => ConsoleColor.Red,
                    LogLevel.Critical => ConsoleColor.Red,
                    LogLevel.None => default,
                    _ => default
                };
                textWriter.WriteWithColor(text, null, consoleColor);
            }
            else
            {
                textWriter.Write(text);
            }
        }

        textWriter.WriteLine();
    }
}