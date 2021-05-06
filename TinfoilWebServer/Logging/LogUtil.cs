using System;
using System.Collections.Generic;
using System.Linq;

namespace TinfoilWebServer.Logging
{
    public static class LogUtil
    {
        public static readonly string MultilineLogSpacing = $"{Environment.NewLine}        ";

        public static string ToMultilineString(this IEnumerable<string> lines)
        {
            return string.Join("", lines.Select(loadingError => $"{MultilineLogSpacing}{loadingError}"));
        }

    }
}
