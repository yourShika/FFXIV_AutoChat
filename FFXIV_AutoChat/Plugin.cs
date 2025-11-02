using System;
using System.Diagnostics;
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

    public Configuration Config { get; private set; } = null!;

    private double elapsed;

    private WindowSystem? windowSystem;
    private Ui.AutoChatWindow? window;

    private Action? openConfigHandler;

    private bool isOperational = true;
    private bool hasFailed;
    private bool commandRegistered;
    private bool frameworkHooked;
    private bool drawHooked;
    private bool openConfigUiHooked;
    private bool openMainUiHooked;

    private const double MaxInitializationSeconds = 10;

    public Plugin(IDalamudPluginInterface PluginInterface)
    {
        PluginInterface.Create<Plugin>(this);

        var initWatch = Stopwatch.StartNew();

        try
        {
            Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Config.Initialize(PluginInterface);
            if (Config.Sanitize())
                Config.Save();

            window = new Ui.AutoChatWindow(this);
            windowSystem = new WindowSystem("AutoChat");
            windowSystem.AddWindow(window);

            PluginInterface.UiBuilder.Draw += DrawUI;
            drawHooked = true;
            openConfigHandler = OpenConfigWindow;
            PluginInterface.UiBuilder.OpenConfigUi += openConfigHandler;
            openConfigUiHooked = true;
            PluginInterface.UiBuilder.OpenMainUi += openConfigHandler;
            openMainUiHooked = true;

            CommandManager.AddHandler(Command, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the AutoChat configuration window."
            });
            commandRegistered = true;

            Framework.Update += OnFrameworkUpdate;
            frameworkHooked = true;
        }
        catch (Exception ex)
        {
            HandleCriticalFailure("initialization failure", ex);
            return;
        }
        finally
        {
            initWatch.Stop();
        }

        if (!hasFailed && initWatch.Elapsed > TimeSpan.FromSeconds(MaxInitializationSeconds))
        {
            HandleCriticalFailure($"initialization timeout after {initWatch.Elapsed.TotalSeconds:F1}s", null);
        }
    }

    private void OnCommand(string command, string args)
    {
        OpenConfigWindow();
    }

    private double tickAccum;
    private const double TickStep = 0.5;

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!isOperational)
            return;

        try
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
        catch (Exception ex)
        {
            HandleCriticalFailure("framework update failure", ex);
        }
    }

    internal void TrySend()
    {
        if (!isOperational)
            return;

        try
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
        catch (Exception ex)
        {
            HandleCriticalFailure("message dispatch failure", ex);
        }
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
        CleanupHooks();
        DisposeWindow();
    }

    private void DrawUI()
    {
        if (!isOperational)
            return;

        try
        {
            windowSystem?.Draw();
        }
        catch (Exception ex)
        {
            HandleCriticalFailure("UI draw failure", ex);
        }
    }

    private void OpenConfigWindow()
    {
        if (!isOperational)
            return;

        if (window != null)
            window.IsOpen = true;
    }

    private void HandleCriticalFailure(string reason, Exception? ex)
    {
        if (hasFailed)
            return;

        hasFailed = true;
        isOperational = false;

        CleanupHooks();
        DisposeWindow();

        tickAccum = 0;
        elapsed = 0;

        if (Config != null && Config.Enabled)
        {
            Config.Enabled = false;
            try
            {
                Config.Save();
            }
            catch
            {
                // ignored - failure to save should not throw further
            }
        }

        var message = $"[AutoChat] Automatic chat disabled due to {reason}.";
        if (ex != null)
        {
            message += $" Error: {ex.Message}";
        }

        ChatGui?.PrintError(message);
    }

    private void CleanupHooks()
    {
        if (frameworkHooked)
        {
            Framework.Update -= OnFrameworkUpdate;
            frameworkHooked = false;
        }

        if (commandRegistered)
        {
            CommandManager.RemoveHandler(Command);
            commandRegistered = false;
        }

        if (drawHooked)
        {
            PluginInterface.UiBuilder.Draw -= DrawUI;
            drawHooked = false;
        }

        if (openConfigUiHooked && openConfigHandler != null)
        {
            PluginInterface.UiBuilder.OpenConfigUi -= openConfigHandler;
            openConfigUiHooked = false;
        }

        if (openMainUiHooked && openConfigHandler != null)
        {
            PluginInterface.UiBuilder.OpenMainUi -= openConfigHandler;
            openMainUiHooked = false;
        }
    }

    private void DisposeWindow()
    {
        windowSystem?.RemoveAllWindows();
        window?.Dispose();
        windowSystem = null;
        window = null;
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
