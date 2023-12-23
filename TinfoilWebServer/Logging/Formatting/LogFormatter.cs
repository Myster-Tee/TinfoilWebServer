using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TinfoilWebServer.Utils.DelimitedTextParsing;

namespace TinfoilWebServer.Logging.Formatting;

public class LogFormatter
{
    private readonly IReadOnlyList<Part> _parts;

    public LogFormatter(IReadOnlyList<Part> parts)
    {
        _parts = parts;
    }

    public string Format<TState>(LogEntry<TState> logEntry)
    {
        var sb = new StringBuilder();
        foreach (var part in _parts)
        {
            sb.Append(part.GetText(logEntry));
        }
        return sb.ToString();
    }

    public static LogFormatter Parse(string logFormat)
    {
        if (logFormat == null)
            throw new ArgumentNullException(nameof(logFormat));

        var parser = new DelimitedTextParser('{', '}', '\\');

        var parts = new List<Part>();
        foreach (var (text, isDelimited) in parser.Parse(logFormat))
        {

            if (isDelimited)
            {
                var split = text.Split(":", 2);
                var kind = split[0].Trim().ToLower();
                var options = split.Length == 2 ? split[1].Trim() : null;

                if (kind.Equals("message", StringComparison.Ordinal))
                {
                    parts.Add(new MessagePart());
                }
                else if (kind.Equals("category", StringComparison.Ordinal))
                {
                    parts.Add(new CategoryPart());
                }
                else if (kind.Equals("eventid", StringComparison.Ordinal))
                {
                    parts.Add(new EventIdPart());
                }
                else if (kind.Equals("loglevel", StringComparison.Ordinal))
                {
                    var logLevelPart = new LogLevelPart();
                    parts.Add(logLevelPart);
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
                    parts.Add(datePart);
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
            }
            else
            {
                parts.Add(new StaticPart(text));
            }
        }

        return new LogFormatter(parts);
    }

    public static LogFormatter Default { get; } = new(new Part[]
    {
        new DatePart(),
        new StaticPart("-"),
        new LogLevelPart(),
        new StaticPart(": "),
        new MessagePart(),
    });
}



internal class DatePart : Part
{
    private string? _format;

    /// <summary>
    /// Throws if format is not valid
    /// </summary>
    public string? Format
    {
        get => _format;
        set
        {
            _ = DateTime.Now.ToString(value);
            _format = value;
        }
    }

    public override string GetText<TState>(LogEntry<TState> logEntry)
    {
        var now = DateTime.Now;

        var format = Format;
        if (format == null)
            return $"{now.Year:0000}-{now.Month:00}-{now.Day:00}T{now.Hour:00}:{now.Minute:00}:{now.Second:00}";
        else
            return now.ToString(format);
    }
}

public abstract class Part
{
    public abstract string GetText<TState>(LogEntry<TState> logEntry);
}

public class StaticPart : Part
{
    public string Text { get; }

    public StaticPart(string text)
    {
        Text = text;
    }

    public override string GetText<TState>(LogEntry<TState> logEntry)
    {
        return Text;
    }
}

public class MessagePart : Part
{
    public override string GetText<TState>(LogEntry<TState> logEntry)
    {
        return logEntry.State?.ToString() ?? "";
    }
}

public class CategoryPart : Part
{
    public override string GetText<TState>(LogEntry<TState> logEntry)
    {
        return logEntry.Category;
    }
}

public class EventIdPart : Part
{
    public override string GetText<TState>(LogEntry<TState> logEntry)
    {
        return logEntry.EventId.Id.ToString();
    }
}

public class LogLevelPart : Part
{
    public LogLevelMode Mode { get; set; } = LogLevelMode.ShortUpper;

    public override string GetText<TState>(LogEntry<TState> logEntry)
    {
        var level = logEntry.LogLevel;
        switch (Mode)
        {
            case LogLevelMode.Upper:
                return level.ToString().ToUpper();
            case LogLevelMode.Lower:
                return level.ToString().ToLower();
            case LogLevelMode.ShortUpper:
                return GetShortUpper(level).ToUpper();
            case LogLevelMode.ShortLower:
                return GetShortUpper(level).ToLower();
            case LogLevelMode.Normal:
            default:
                return level.ToString();
        }

    }


    public string GetShortUpper(LogLevel logLevel)
    {
        switch (logLevel)
        {
            case LogLevel.Trace:
                return "[T]";
            case LogLevel.Debug:
                return "[D]";
            case LogLevel.Information:
                return "[I]";
            case LogLevel.Warning:
                return "[W]";
            case LogLevel.Error:
                return "[E]";
            case LogLevel.Critical:
                return "[C]";
            default:
                return "[U]"; // Unknown
        }
    }
}

public enum LogLevelMode
{
    Normal,
    Upper,
    Lower,
    ShortUpper,
    ShortLower,
}