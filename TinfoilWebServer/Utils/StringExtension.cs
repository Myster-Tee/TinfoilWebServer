namespace TinfoilWebServer.Utils;

public static class StringExtension
{
    public static string? Truncate(this string? str, int maxLength)
    {
        if (str == null)
            return null;

        return str.Length > maxLength ? str[0..maxLength] : str;
    }
}