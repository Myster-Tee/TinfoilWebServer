using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using TinfoilWebServer.Logging.Formatting.BasePartModels;
using TinfoilWebServer.Logging.Formatting.LogEntryPartModels;

namespace TinfoilWebServer.Logging.Formatting;

public static class LogEntryExtension
{
    [Pure]
    public static IEnumerable<Tuple<string?, IPart>> FormatParts<TState>(this LogEntry<TState> logEntry, LogEntryFormat format)
    {
        if (format == null)
            throw new ArgumentNullException(nameof(format));


        foreach (var part in format.LogEntryParts)
        {
            if (part is ExceptionLogEntryPart)
            {
                if (logEntry.Exception != null)
                {
                    var exTuples = format.ExParts.Select(exPart => new Tuple<string?, IPart>(exPart.GetText(logEntry.Exception), exPart));
                    foreach (var build in exTuples)
                    {
                        yield return build;
                    }
                }
            }
            else
            {
                yield return new Tuple<string?, IPart>(part.GetText(logEntry), part);
            }
        }
    }

    [Pure]
    public static string Format<TState>(this LogEntry<TState> logEntry, LogEntryFormat format)
    {
        var sb = new StringBuilder();

        foreach (var (text, _) in logEntry.FormatParts(format))
        {
            sb.Append(text);
        }

        return sb.ToString();
    }

}