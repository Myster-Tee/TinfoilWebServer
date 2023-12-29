using System;

namespace TinfoilWebServer.Logging.Formatting.BasePartModels;

public class DateBasePart
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

    public string GetDateString()
    {
        var now = DateTime.Now;

        var format = Format;
        if (format == null)
            return $"{now.Year:0000}-{now.Month:00}-{now.Day:00}T{now.Hour:00}:{now.Minute:00}:{now.Second:00}";
        else
            return now.ToString(format);
    }
}