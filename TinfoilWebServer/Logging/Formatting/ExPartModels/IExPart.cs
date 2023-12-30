using System;
using TinfoilWebServer.Logging.Formatting.BasePartModels;

namespace TinfoilWebServer.Logging.Formatting.ExPartModels;

public interface IExPart : IPart
{
    public  string? GetText(Exception ex);
}