using System;

namespace TinfoilWebServer.Logging.Formatting.ExPartModels;

public class TypeExPart : IExPart
{
    public string GetText(Exception ex)
    {
        return ex.GetType().Name;
    }
}