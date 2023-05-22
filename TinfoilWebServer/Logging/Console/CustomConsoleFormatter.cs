using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace TinfoilWebServer.Logging.Console;

public class CustomConsoleFormatter : ConsoleFormatter
{
    public CustomConsoleFormatter() : base(nameof(CustomConsoleFormatter))
    {

    }
     

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
    {
        var message = logEntry.State.ToString();
        if (message == null)
            return;

        if (logEntry.Exception != null)
            message += Environment.NewLine + logEntry.Exception.Message;

        switch (logEntry.LogLevel)
        {
            case LogLevel.Trace:
                textWriter.WriteWithColor("[T] ", null, ConsoleColor.Magenta);
                break;
            case LogLevel.Debug:
                textWriter.WriteWithColor("[D] ", null, ConsoleColor.Magenta);
                break;
            case LogLevel.Information:
                textWriter.WriteWithColor("[I] ", null, ConsoleColor.Green);
                break;
            case LogLevel.Warning:
                textWriter.WriteWithColor("[W] ", null, ConsoleColor.Yellow);
                break;
            case LogLevel.Error:
                textWriter.WriteWithColor("[E] ", null, ConsoleColor.Red);
                break;
            case LogLevel.Critical:
                textWriter.WriteWithColor("[C] ", null, ConsoleColor.Red);
                break;
            case LogLevel.None:
                break;
            default:
                break;
        }

        textWriter.WriteLine(message);
    }
}