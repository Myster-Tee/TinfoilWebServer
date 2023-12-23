using System;

namespace TinfoilWebServer.Utils.DelimitedTextParsing;

/// <summary>
/// Helper which stores the previously read char
/// </summary>
public class StringReader : IDisposable
{
    private readonly System.IO.StringReader _sr;
    private char _lastReadChar = '\0';

    public StringReader(string str)
    {
        _sr = new System.IO.StringReader(str);
    }

    public char PrevChar { get; private set; } = '\0';

    public bool ReadChar(out char c)
    {
        var ci = _sr.Read();
        if (ci < 0)
        {
            c = default;
            return false;
        }

        PrevChar = _lastReadChar;
        c = (char)ci;
        _lastReadChar = c;
        return true;
    }

    public void Dispose()
    {
        _sr.Dispose();
    }
}