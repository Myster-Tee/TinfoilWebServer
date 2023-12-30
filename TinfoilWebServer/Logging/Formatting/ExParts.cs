using System;
using System.Collections.Generic;
using TinfoilWebServer.Logging.Formatting.ExPartModels;
using TinfoilWebServer.Utils.DelimitedTextParsing;

namespace TinfoilWebServer.Logging.Formatting;

public class ExParts : List<IExPart>
{
    public static ExParts ParseException(string exceptionFormat)
    {
        if (exceptionFormat == null)
            throw new ArgumentNullException(nameof(exceptionFormat));

        var parser = new DelimitedTextParser('{', '}', '\\');

        var parts = new ExParts();
        foreach (var (text, isDelimited) in parser.Parse(exceptionFormat))
        {
            var split = text.Split(":", 2);
            var kind = split[0].Trim().ToLower();
            var options = split.Length == 2 ? split[1].Trim() : null;

            if (isDelimited)
            {
                if (kind.Equals("newline", StringComparison.Ordinal))
                {
                    parts.Add(new NewLineExPart());
                }
                else if (kind.Equals("message", StringComparison.Ordinal))
                {
                    parts.Add(new MessageExPart());
                }
                else if (kind.Equals("type", StringComparison.Ordinal))
                {
                    parts.Add(new TypeExPart());
                }
                else if (kind.Equals("stacktrace", StringComparison.Ordinal))
                {
                    parts.Add(new StackTraceExPart());
                }
                else if (kind.Equals("date", StringComparison.Ordinal))
                {
                    var dateExPart = new DateExPart();
                    parts.Add(dateExPart);
                    if (options != null)
                    {
                        try
                        {
                            dateExPart.Format = options;
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
                parts.Add(new StaticExPart(kind));
            }
        }

        return parts;
    }
}