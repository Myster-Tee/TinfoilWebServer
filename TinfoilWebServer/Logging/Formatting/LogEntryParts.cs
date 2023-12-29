using System;
using System.Collections.Generic;
using TinfoilWebServer.Logging.Formatting.LogEntryPartModels;
using TinfoilWebServer.Utils.DelimitedTextParsing;

namespace TinfoilWebServer.Logging.Formatting;

public class LogEntryParts : List<ILogEntryPart>
{
    public static LogEntryParts Parse(string format)
    {
        if (format == null)
            throw new ArgumentNullException(nameof(format));

        var parser = new DelimitedTextParser('{', '}', '\\');

        var parts = new LogEntryParts();
        foreach (var (text, isDelimited) in parser.Parse(format))
        {
            if (isDelimited)
            {
                var split = text.Split(":", 2);
                var kind = split[0].Trim().ToLower();
                var options = split.Length == 2 ? split[1].Trim() : null;

                if (kind.Equals("message", StringComparison.Ordinal))
                {
                    parts.Add(new MessageLogEntryPart());
                }
                else if (kind.Equals("newline", StringComparison.Ordinal))
                {
                    parts.Add(new NewLineLogEntryPart());
                }
                else if (kind.Equals("category", StringComparison.Ordinal))
                {
                    parts.Add(new CategoryLogEntryPart());
                }
                else if (kind.Equals("eventid", StringComparison.Ordinal))
                {
                    parts.Add(new EventIdLogEntryPart());
                }
                else if (kind.Equals("loglevel", StringComparison.Ordinal))
                {
                    var logLevelLogEntryPart = new LogLevelLogEntryPart();
                    parts.Add(logLevelLogEntryPart);
                    if (options != null)
                    {
                        var optionsLower = options.ToLower();

                        if (optionsLower.Equals("su", StringComparison.Ordinal))
                        {
                            logLevelLogEntryPart.Mode = LogLevelMode.ShortUpper;
                        }
                        else if (optionsLower.Equals("sl", StringComparison.Ordinal))
                        {
                            logLevelLogEntryPart.Mode = LogLevelMode.ShortLower;
                        }
                        else if (optionsLower.Equals("l", StringComparison.Ordinal))
                        {
                            logLevelLogEntryPart.Mode = LogLevelMode.Lower;
                        }
                        else if (optionsLower.Equals("u", StringComparison.Ordinal))
                        {
                            logLevelLogEntryPart.Mode = LogLevelMode.Upper;
                        }
                    }
                }
                else if (kind.Equals("date", StringComparison.Ordinal))
                {
                    var dateLogEntryPart = new DateLogEntryPart();
                    parts.Add(dateLogEntryPart);
                    if (options != null)
                    {
                        try
                        {
                            dateLogEntryPart.Format = options;
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
                else if (kind.Equals("exception", StringComparison.Ordinal))
                {
                    parts.Add(new ExceptionLogEntryPart());
                }
            }
            else
            {
                parts.Add(new StaticLogEntryPart(text));
            }
        }

        return parts;
    }
}