using System;
using Dalamud.Configuration;
using Dalamud.Logging;
using Dalamud.Plugin;

namespace AutoChat;

public sealed class Configuration : IPluginConfiguration
{
    public const int MaxMessageLength = 500;

    public int Version { get; set; } = 1;

    public bool Enabled { get; set; } = false;
    public string Message { get; set; } = "Hello, world!";
    public int IntervalSeconds { get; set; } = 300; // 5 Minutes
    public ChatChannel Channel { get; set; } = ChatChannel.Say;

    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pi) => pluginInterface = pi;

    public void Save() => pluginInterface?.SavePluginConfig(this);

    public bool Sanitize()
    {
        var changed = false;

        var normalizedMessage = NormalizeMessage(Message);
        if (!string.Equals(Message, normalizedMessage, StringComparison.Ordinal))
        {
            Message = normalizedMessage;
            PluginLog.LogDebug($"Configuration sanitized message to '{Message}'.");
            changed = true;
        }

        var clamped = Math.Clamp(IntervalSeconds, 5, 3600);
        if (clamped != IntervalSeconds)
        {
            IntervalSeconds = clamped;
            PluginLog.LogDebug($"Configuration clamped interval to {IntervalSeconds}s.");
            changed = true;
        }

        return changed;
    }

    public static string NormalizeMessage(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Replace carriage returns/new lines with single spaces to keep a single-line chat message
        var normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal)
                              .Replace('\r', '\n');

        if (normalized.IndexOf('\n') >= 0)
            normalized = normalized.Replace('\n', ' ');

        normalized = normalized.Trim();

        if (normalized.Length > MaxMessageLength)
            normalized = normalized[..MaxMessageLength];

        return normalized;
    }
}
