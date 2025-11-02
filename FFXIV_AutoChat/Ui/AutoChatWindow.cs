using System;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ImGuiNET;

namespace AutoChat.Ui;

public sealed class AutoChatWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public AutoChatWindow(Plugin plugin) : base("AutoChat - Configuration")
    {
        this.plugin = plugin;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(420, 260),
            MaximumSize = new(900, 600),
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (!IsOpen) return;

        var cfg = plugin.Config;
        if (cfg.Sanitize())
        {
            PluginLog.LogDebug("[AutoChat] Configuration sanitized via window draw; persisting changes.");
            cfg.Save();
        }

        bool enabled = cfg.Enabled;
        if (ImGui.Checkbox("Enable (Start/Stop)", ref enabled))
        {
            cfg.Enabled = enabled;
            cfg.Save();
            PluginLog.LogInformation($"[AutoChat] Plugin {(enabled ? "enabled" : "disabled")} from configuration window.");
        }

        ImGui.Separator();

        //Message
        var msg = cfg.Message ?? string.Empty;
        ImGui.Text("Message to Send");
        if (ImGui.InputTextMultiline("##msg", ref msg, 2048, new System.Numerics.Vector2(-1, 80)))
        {
            var normalized = Configuration.NormalizeMessage(msg);
            if (!string.Equals(normalized, cfg.Message, StringComparison.Ordinal))
            {
                cfg.Message = normalized;
                cfg.Save();
                PluginLog.LogDebug("[AutoChat] Configuration message updated via window.");
            }

            if (!string.Equals(normalized, msg, StringComparison.Ordinal))
                msg = normalized;
        }

        var currentLength = (cfg.Message ?? string.Empty).Length;
        ImGui.TextDisabled($"{currentLength}/{Configuration.MaxMessageLength} characters (max)");
        if (currentLength >= Configuration.MaxMessageLength)
        {
            ImGui.SameLine();
            ImGui.TextColored(new System.Numerics.Vector4(1f, 0.6f, 0f, 1f), "Limit reached");
        }

        //Interval
        int interval = cfg.IntervalSeconds;
        ImGui.Text("Interval (Seconds)");
        if (ImGui.SliderInt("##interval", ref interval, 5, 3600))
        {
            cfg.IntervalSeconds = Math.Clamp(interval, 5, 3600);
            cfg.Save();
            PluginLog.LogDebug($"[AutoChat] Interval updated to {cfg.IntervalSeconds}s via window.");
        }

        //Channel
        ImGui.Text("Chat-Channel");
        if (ImGui.BeginCombo("##channel", cfg.Channel.ToString()))
        {
            foreach (ChatChannel c in Enum.GetValues(typeof(ChatChannel)))
            {
                bool sel = c == cfg.Channel;
                if (ImGui.Selectable(c.ToString(), sel))
                {
                    cfg.Channel = c;
                    cfg.Save();
                    PluginLog.LogDebug($"[AutoChat] Channel changed to {cfg.Channel} via window.");
                }
                if (sel) ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }

        ImGui.Separator();

        // End of Window
        if (ImGui.Button("Send now"))
        {
            PluginLog.LogDebug("[AutoChat] Manual send triggered via configuration window.");
            plugin.TrySend();
        }

        ImGui.SameLine();
        if (ImGui.Button(cfg.Enabled ? "Stop" : "Start"))
        {
            cfg.Enabled = !cfg.Enabled;
            cfg.Save();
            PluginLog.LogInformation($"[AutoChat] Plugin {(cfg.Enabled ? "enabled" : "disabled")} via quick toggle button.");
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Please note that excessive automated messaging may violate game policies. Use responsibly.");
    }
}