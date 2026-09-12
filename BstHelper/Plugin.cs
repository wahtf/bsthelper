using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using BstHelper.Bestiary;
using BstHelper.Travel;
using BstHelper.Windows;

namespace BstHelper;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IAetheryteList AetheryteList { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;

    internal static Configuration Configuration { get; private set; } = null!;

    private const string CommandName = "/bst";
    private const string CommandAlias = "/bsthelper";

    public readonly WindowSystem WindowSystem = new("BstHelper");
    public readonly BeastTrip Trip = new();

    private readonly BestiaryWindow bestiaryWindow;
    private readonly TrailOverlay trailOverlay;
    private readonly DebugWindow debugWindow;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        IpcHub.Init();
        CaptureSync.Init();

        bestiaryWindow = new BestiaryWindow(this);
        trailOverlay = new TrailOverlay(this, bestiaryWindow);
        debugWindow = new DebugWindow();
        WindowSystem.AddWindow(bestiaryWindow);
        WindowSystem.AddWindow(trailOverlay);
        WindowSystem.AddWindow(debugWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the bestiary. \"/bst <number|name>\" travels to that beast, \"/bst stop\" cancels",
        });
        CommandManager.AddHandler(CommandAlias, new CommandInfo(OnCommand)
        {
            HelpMessage = "Alias of /bst.",
        });

        PluginInterface.UiBuilder.Draw += DrawUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleMainUi;
        Framework.Update += OnUpdate;
    }

    public void Dispose()
    {
        Framework.Update -= OnUpdate;
        PluginInterface.UiBuilder.Draw -= DrawUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleMainUi;

        CaptureSync.Dispose();
        Trip.Stop("Plugin unloading");
        WindowSystem.RemoveAllWindows();
        bestiaryWindow.Dispose();
        trailOverlay.Dispose();
        debugWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler(CommandAlias);
    }

    private void DrawUi()
    {
        if (PlayerActions.InCutscene)
            return;

        WindowSystem.Draw();
    }

    private void OnUpdate(IFramework framework)
    {
        CaptureSync.Update();
        Trip.Update();
    }

    public void ToggleMainUi() => bestiaryWindow.Toggle();

    private void OnCommand(string command, string args)
    {
        var argument = args.Trim();

        if (argument.Length == 0)
        {
            ToggleMainUi();
            return;
        }

        if (argument.Equals("debug", System.StringComparison.OrdinalIgnoreCase))
        {
            debugWindow.Toggle();
            return;
        }

        if (argument.Equals("stop", System.StringComparison.OrdinalIgnoreCase))
        {
            Trip.Stop("Asked to stop");
            ChatGui.Print("[BstHelper] Stopped.");
            return;
        }

        if (BeastTable.Find(argument) is not { } beast)
        {
            ChatGui.PrintError($"[BstHelper] No beast matches \"{argument}\".");
            return;
        }

        if (!beast.IsOverworld)
        {
            ChatGui.Print($"[BstHelper] No. {beast.Number} {beast.Name} is a {beast.DutyRole} in {beast.Duty}; there is nowhere to walk to.");
            return;
        }

        Trip.Start(beast);
    }
}
