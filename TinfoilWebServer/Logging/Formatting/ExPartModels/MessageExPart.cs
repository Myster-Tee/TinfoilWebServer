using System;

namespace TinfoilWebServer.Logging.Formatting.ExPartModels;

public class MessageExPart : IExPart
{
    public string GetText(Exception ex)
    {
        return ex.Message;
    }
}