using System;

namespace TinfoilWebServer.Logging.Formatting.ExPartModels;

public class StaticExPart : IExPart
{
    public string Text { get; }

    public StaticExPart(string text)
    {
        Text = text;
    }

    public string GetText(Exception ex)
    {
        return Text;
    }
}