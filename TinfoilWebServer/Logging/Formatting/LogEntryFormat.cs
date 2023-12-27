using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using TinfoilWebServer.Logging.Formatting.Parts;
using TinfoilWebServer.Utils.DelimitedTextParsing;

namespace TinfoilWebServer.Logging.Formatting;


/// <summary>
/// Root model of the definition for the formatting of a <see cref="LogEntry{TState}"/>.
/// This model consists in a list of <see cref="Part"/>.
/// </summary>
public class LogEntryFormat : List<Part>
{

    public static LogEntryFormat Parse(string logFormat)
    {
        if (logFormat == null)
            throw new ArgumentNullException(nameof(logFormat));

        var parser = new DelimitedTextParser('{', '}', '\\');

        var logEntryFormat = new LogEntryFormat();
        foreach (var (text, isDelimited) in parser.Parse(logFormat))
        {
            if (isDelimited)
            {
                var split = text.Split(":", 2);
                var kind = split[0].Trim().ToLower();
                var options = split.Length == 2 ? split[1].Trim() : null;

                if (kind.Equals("message", StringComparison.Ordinal))
                {
                    logEntryFormat.Add(new MessagePart());
                }
                else if (kind.Equals("newline", StringComparison.Ordinal))
                {
                    logEntryFormat.Add(new NewLinePart());
                }
                else if (kind.Equals("category", StringComparison.Ordinal))
                {
                    logEntryFormat.Add(new CategoryPart());
                }
                else if (kind.Equals("eventid", StringComparison.Ordinal))
                {
                    logEntryFormat.Add(new EventIdPart());
                }
                else if (kind.Equals("loglevel", StringComparison.Ordinal))
                {
                    var logLevelPart = new LogLevelPart();
                    logEntryFormat.Add(logLevelPart);
                    if (options != null)
                    {
                        var optionsLower = options.ToLower();

                        if (optionsLower.Equals("su", StringComparison.Ordinal))
                        {
                            logLevelPart.Mode = LogLevelMode.ShortUpper;
                        }
                        else if (optionsLower.Equals("sl", StringComparison.Ordinal))
                        {
                            logLevelPart.Mode = LogLevelMode.ShortLower;
                        }
                        else if (optionsLower.Equals("l", StringComparison.Ordinal))
                        {
                            logLevelPart.Mode = LogLevelMode.Lower;
                        }
                        else if (optionsLower.Equals("u", StringComparison.Ordinal))
                        {
                            logLevelPart.Mode = LogLevelMode.Upper;
                        }
                    }
                }
                else if (kind.Equals("date", StringComparison.Ordinal))
                {
                    var datePart = new DatePart();
                    logEntryFormat.Add(datePart);
                    if (options != null)
                    {
                        try
                        {
                            datePart.Format = options;
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
                else if (kind.Equals("exmessage", StringComparison.Ordinal))
                {
                    var exMessagePart = new ExMessagePart();
                    logEntryFormat.Add(exMessagePart);
                }
                else if (kind.Equals("extype", StringComparison.Ordinal))
                {
                    var exTypePart = new ExTypePart();
                    logEntryFormat.Add(exTypePart);
                }
                else if (kind.Equals("exstacktrace", StringComparison.Ordinal))
                {
                    var exStackTracePart = new ExStackTracePart();
                    logEntryFormat.Add(exStackTracePart);
                }
            }
            else
            {
                logEntryFormat.Add(new StaticPart(text));
            }
        }

        return logEntryFormat;
    }


    /// <summary>
    /// The default format for <see cref="LogEntry{TState}"/> without <see cref="LogEntry{TState}.Exception"/>.
    /// </summary>
    public static LogEntryFormat Default =>
        new()
        {
            new DatePart(), new StaticPart(" "), new LogLevelPart(), new StaticPart(": "), new MessagePart()
        };

    /// <summary>
    /// The default format for <see cref="LogEntry{TState}"/> with <see cref="LogEntry{TState}.Exception"/>.
    /// </summary>
    public static LogEntryFormat DefaultWithException =>
        new()
        {
            new DatePart(), new StaticPart(" "), new LogLevelPart(), new StaticPart(": "), new MessagePart(), new NewLinePart(),
            new StaticPart("Exception Type: "), new ExTypePart(), new NewLinePart(),
            new StaticPart("Exception Message: "), new ExMessagePart(), new NewLinePart(),
            new StaticPart("Exception Stack Trace: "), new ExStackTracePart()
        };
}