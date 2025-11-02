using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace AutoChat;

public sealed class Configuration : IPluginConfiguration
{
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

        if (Message is null)
        {
            Message = string.Empty;
            changed = true;
        }

        var clamped = Math.Clamp(IntervalSeconds, 5, 3600);
        if (clamped != IntervalSeconds)
        {
            IntervalSeconds = clamped;
            changed = true;
        }

        return changed;
    }
}
