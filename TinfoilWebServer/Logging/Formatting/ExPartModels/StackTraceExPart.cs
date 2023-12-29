using System;

namespace TinfoilWebServer.Logging.Formatting.ExPartModels;

public class StackTraceExPart : IExPart
{
    public string? GetText(Exception ex)
    {
        return ex.StackTrace;
    }
}