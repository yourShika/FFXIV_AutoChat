using System;
using System.Diagnostics;
using System.Globalization;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Logging;

namespace AutoChat;

internal static class PluginLog
{
    private static IPluginLog? logger;

    internal static void Initialize(IPluginLog pluginLog)
    {
        logger = pluginLog ?? throw new ArgumentNullException(nameof(pluginLog));
    }

    internal static void LogTrace(string message, params object[] args)
        => Log(l => l.LogTrace(message, args), "TRACE", message, args);

    internal static void LogDebug(string message, params object[] args)
        => Log(l => l.LogDebug(message, args), "DEBUG", message, args);

    internal static void LogInformation(string message, params object[] args)
        => Log(l => l.LogInformation(message, args), "INFO", message, args);

    internal static void LogError(string message, params object[] args)
        => Log(l => l.LogError(message, args), "ERROR", message, args);

    internal static void LogError(Exception exception, string message, params object[] args)
    {
        if (logger is { } log)
        {
            log.LogError(exception, message, args);
        }
        else
        {
            WriteFallback("ERROR", FormatMessage(message, args) + $" Exception: {exception}");
        }
    }

    private static void Log(Action<IPluginLog> logAction, string level, string message, object[] args)
    {
        if (logger is { } log)
        {
            logAction(log);
        }
        else
        {
            WriteFallback(level, FormatMessage(message, args));
        }
    }

    private static void WriteFallback(string level, string message)
    {
        Debug.WriteLine($"[AutoChat][{level}] {message}");
    }

    private static string FormatMessage(string message, object[] args)
    {
        if (args.Length == 0)
            return message;

        try
        {
            return string.Format(CultureInfo.InvariantCulture, message, args);
        }
        catch
        {
            return message;
        }
    }
}
