using System.Collections.Generic;
using System.Text;

namespace TinfoilWebServer.Utils;

public static class BytesExtension
{
    public static string ToHexString(this IEnumerable<byte> bytes, bool upperCase = true)
    {
        var format = upperCase ? "X2" : "x2";

        var sb = new StringBuilder();
        foreach (var b in bytes)
        {
            sb.Append(b.ToString(format));
        }
        return sb.ToString();
    }

}