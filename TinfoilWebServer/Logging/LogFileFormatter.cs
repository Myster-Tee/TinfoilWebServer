using System;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;

namespace TinfoilWebServer.Logging;

public static class LogFileFormatter
{
    public static string FormatLogEntry(LogMessage message)
    {
        var exceptionMessage = "";
        var ex = message.Exception;
        if (ex != null)
            exceptionMessage +=
                $"{Environment.NewLine}" +
                $"Exception Type: {ex.GetType().Name}{Environment.NewLine}" +
                $"Stack Trace:{Environment.NewLine}{ex.StackTrace}";

        return $"{DateTime.Now}-{LevelToString(message.LogLevel)}: {message.Message}{exceptionMessage}";
    }

    private static string LevelToString(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRITICAL",
            LogLevel.None => "NONE",
            _ => level.ToString()
        };
    }
}