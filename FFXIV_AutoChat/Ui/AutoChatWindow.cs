using System;
using Dalamud.Interface.Windowing;
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

        bool enabled = cfg.Enabled;
        if (ImGui.Checkbox("Enable (Start/Stop)", ref enabled))
        {
            cfg.Enabled = enabled;
            cfg.Save();
        }

        ImGui.Separator();

        //Message
        var msg = cfg.Message ?? string.Empty;
        ImGui.Text("Message to Send");
        ImGui.InputTextMultiline("##msg", ref msg, 2048, new System.Numerics.Vector2(-1, 80));
        if (msg != cfg.Message)
        {
            cfg.Message = msg;
            cfg.Save();
        }

        //Interval
        int interval = cfg.IntervalSeconds;
        ImGui.Text("Interval (Seconds)");
        if (ImGui.SliderInt("##interval", ref interval, 5, 3600))
        {
            cfg.IntervalSeconds = Math.Clamp(interval, 5, 3600);
            cfg.Save();
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
                }
                if (sel) ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }

        ImGui.Separator();

        // End of Window
        if (ImGui.Button("Send now"))
        {
            plugin.TrySend();
        }

        ImGui.SameLine();
        if (ImGui.Button(cfg.Enabled ? "Stop" : "Start"))
        {
            cfg.Enabled = !cfg.Enabled;
            cfg.Save();
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Please note that excessive automated messaging may violate game policies. Use responsibly.");
    }
}