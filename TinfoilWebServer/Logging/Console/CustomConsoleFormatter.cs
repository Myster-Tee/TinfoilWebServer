using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using TinfoilWebServer.Logging.Formatting;
using TinfoilWebServer.Logging.Formatting.Parts;


namespace TinfoilWebServer.Logging.Console;

public class CustomConsoleFormatter : ConsoleFormatter
{
    private LogEntryFormat _logEntryFormat;
    private LogEntryFormat _logEntryWithExceptionFormat;
    private bool _useColor = true;

    public CustomConsoleFormatter(IOptionsMonitor<CustomConsoleFormatterOptions> optionsMonitor) : base(nameof(CustomConsoleFormatter))
    {
        UpdateFromOptions(optionsMonitor.CurrentValue);
        optionsMonitor.OnChange(UpdateFromOptions);
    }

    [MemberNotNull(nameof(_logEntryFormat))]
    [MemberNotNull(nameof(_logEntryWithExceptionFormat))]
    private void UpdateFromOptions(CustomConsoleFormatterOptions options)
    {
        var format = options.Format;
        _logEntryFormat = string.IsNullOrEmpty(format) ? LogEntryFormat.Default : LogEntryFormat.Parse(format);

        var formatWithException = options.FormatWithException;
        _logEntryWithExceptionFormat = string.IsNullOrEmpty(formatWithException) ? LogEntryFormat.DefaultWithException: LogEntryFormat.Parse(formatWithException);

        _useColor = options.UseColor;
    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        var logEntryFormat = logEntry.Exception != null ? _logEntryWithExceptionFormat: _logEntryFormat;

        foreach (var part in logEntryFormat)
        {
            var text = part.GetText(logEntry);
            if (_useColor && part is LogLevelPart && text != null)
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