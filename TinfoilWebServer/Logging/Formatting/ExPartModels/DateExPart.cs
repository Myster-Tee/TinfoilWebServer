using System;
using TinfoilWebServer.Logging.Formatting.BasePartModels;

namespace TinfoilWebServer.Logging.Formatting.ExPartModels;

public class DateExPart : DateBasePart, IExPart
{
    public string GetText(Exception ex)
    {
        return GetDateString();
    }
}