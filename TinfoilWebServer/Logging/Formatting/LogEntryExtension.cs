using System;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;

namespace TinfoilWebServer.Logging.Formatting;

public static class LogEntryExtension
{

    public static string Format<TState>(this LogEntry<TState> logEntry, LogEntryFormat format)
    {
        if (format == null)
            throw new ArgumentNullException(nameof(format));

        var sb = new StringBuilder();
        foreach (var part in format)
        {
            sb.Append(part.GetText(logEntry));
        }
        return sb.ToString();
    }

}