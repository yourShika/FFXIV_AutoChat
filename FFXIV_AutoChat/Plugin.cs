using System;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;

namespace AutoChat;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "AutoChat";

    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    private const string Command = "/autochat";

    public Configuration Config { get; private set; }

    private double elapsed;

    private readonly WindowSystem windowSystem;
    private readonly Ui.AutoChatWindow window;

    private readonly Action openConfigHandler;

    public Plugin(IDalamudPluginInterface PluginInterface)
    {
        PluginInterface.Create<Plugin>(this);

        Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Initialize(PluginInterface);
        if (Config.Sanitize())
            Config.Save();

        window = new Ui.AutoChatWindow(this);
        windowSystem = new WindowSystem("AutoChat");
        windowSystem.AddWindow(window);

        PluginInterface.UiBuilder.Draw += DrawUI;
        openConfigHandler = OpenConfigWindow;
        PluginInterface.UiBuilder.OpenConfigUi += openConfigHandler;
        PluginInterface.UiBuilder.OpenMainUi += openConfigHandler;

        CommandManager.AddHandler(Command, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the AutoChat configuration window."
        });

        Framework.Update += OnFrameworkUpdate;
    }

    private void OnCommand(string command, string args)
    {
        OpenConfigWindow();
    }

    private double tickAccum;
    private const double TickStep = 0.5;

    private void OnFrameworkUpdate(IFramework framework)
    {
        tickAccum += framework.UpdateDelta.TotalSeconds;
        if (tickAccum < TickStep)
            return;

        var step = tickAccum;
        tickAccum = 0;

        if (!Config.Enabled || Config.IntervalSeconds < 1 || !ClientState.IsLoggedIn)
        {
            elapsed = 0;
            return;
        }

        elapsed += step;
        if (elapsed + 1e-6 >= Config.IntervalSeconds) // numeric safety
        {
            elapsed = 0;
            TrySend();
        }
    }

    internal void TrySend()
    {
        var msg = (Config.Message ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(msg)) return;

        var prefix = GetChannelPrefix(Config.Channel);
        if (string.IsNullOrEmpty(prefix))
        {
            ChatGui.Print(msg);
            return;
        }

        var toSend = $"{prefix} {msg}";
        CommandManager.ProcessCommand(toSend);
    }

    public static string GetChannelPrefix(ChatChannel channel) => channel switch
    {
        ChatChannel.Say => "/s",
        ChatChannel.Shout => "/sh",
        ChatChannel.Yell => "/y",
        ChatChannel.Party => "/p",
        ChatChannel.Alliance => "/a",
        ChatChannel.FreeCompany => "/fc",
        ChatChannel.Linkshell1 => "/l1",
        ChatChannel.Linkshell2 => "/l2",
        ChatChannel.Linkshell3 => "/l3",
        ChatChannel.Linkshell4 => "/l4",
        ChatChannel.Linkshell5 => "/l5",
        ChatChannel.Linkshell6 => "/l6",
        ChatChannel.Linkshell7 => "/l7",
        ChatChannel.Linkshell8 => "/l8",
        ChatChannel.CrossWorldLinkshell1 => "/cl1",
        ChatChannel.CrossWorldLinkshell2 => "/cl2",
        ChatChannel.CrossWorldLinkshell3 => "/cl3",
        ChatChannel.CrossWorldLinkshell4 => "/cl4",
        ChatChannel.CrossWorldLinkshell5 => "/cl5",
        ChatChannel.CrossWorldLinkshell6 => "/cl6",
        ChatChannel.CrossWorldLinkshell7 => "/cl7",
        ChatChannel.CrossWorldLinkshell8 => "/cl8",
        ChatChannel.YellowText => string.Empty,
        _ => string.Empty,
    };

    public void Dispose()
    {
        Framework.Update -= OnFrameworkUpdate;
        CommandManager.RemoveHandler(Command);
        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= openConfigHandler;
        PluginInterface.UiBuilder.OpenMainUi -= openConfigHandler;

        windowSystem.RemoveAllWindows();
        window?.Dispose();
    }

    private void DrawUI()
    {
        windowSystem.Draw();
    }

    private void OpenConfigWindow()
    {
        window.IsOpen = true;
    }
}

public enum ChatChannel
{
    Say,
    Shout,
    Yell,
    Party,
    Alliance,
    FreeCompany,
    Linkshell1,
    Linkshell2,
    Linkshell3,
    Linkshell4,
    Linkshell5,
    Linkshell6,
    Linkshell7,
    Linkshell8,
    CrossWorldLinkshell1,
    CrossWorldLinkshell2,
    CrossWorldLinkshell3,
    CrossWorldLinkshell4,
    CrossWorldLinkshell5,
    CrossWorldLinkshell6,
    CrossWorldLinkshell7,
    CrossWorldLinkshell8,
    YellowText
}
