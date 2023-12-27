using System;
using Microsoft.Extensions.Logging.Abstractions;

namespace TinfoilWebServer.Logging.Formatting.Parts;

public class NewLinePart : Part
{
    public override string GetText<TState>(LogEntry<TState> logEntry)
    {
        return Environment.NewLine;
    }
}