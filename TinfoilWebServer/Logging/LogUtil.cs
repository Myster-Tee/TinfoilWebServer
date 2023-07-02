using System;
using System.Collections.Generic;
using System.Linq;

namespace TinfoilWebServer.Logging;

public static class LogUtil
{
    public const string INDENT_SPACES = $"        ";

    public static string ToMultilineString(this IEnumerable<string> lines)
    {
        return string.Join("", lines.Select(loadingError => $"{Environment.NewLine}{INDENT_SPACES}{loadingError}"));
    }

}