using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using DuckbillRelic.Core.Catalog;
using DuckbillRelic.Plugin.Game;
using DuckbillRelic.Plugin.Localization;
using DuckbillRelic.Plugin.UI;

namespace DuckbillRelic.Plugin;

public sealed class Plugin : IDalamudPlugin
{
    private const string Command = "/duckbillrelic";
    private const string ShortCommand = "/dbrl";
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commandManager;
    private readonly WindowSystem windows = new("DuckbillRelic");
    private readonly ProgressObserver observer;
    private readonly MainWindow mainWindow;
    private readonly bool shortCommandRegistered;

    public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager, IDataManager dataManager,
        IFramework framework, IClientState clientState, IPlayerState playerState, IGameInventory inventory,
        IUnlockState unlockState, ITextureProvider textures, IPluginLog log)
    {
        this.pluginInterface = pluginInterface;
        this.commandManager = commandManager;
        UiText.SetLanguage(UiText.DefaultFor(dataManager.Language, pluginInterface.UiLanguage));

        var bundled = LoadBundledCatalog();
        var disabled = CatalogValidator.FindMismatches(dataManager, bundled, log);
        var catalog = disabled.Count == 0 ? bundled : bundled.Without(disabled);
        var names = new GameNames(dataManager, bundled);

        observer = new ProgressObserver(catalog, framework, clientState, playerState, inventory, unlockState, dataManager,
            new ProgressStore(pluginInterface.GetPluginConfigDirectory(), log));
        mainWindow = new MainWindow(catalog, names, observer, textures, disabled);
        windows.AddWindow(mainWindow);

        var info = new CommandInfo(OnCommand) { HelpMessage = UiText.Get(TextKey.CommandHelp) };
        if (!commandManager.AddHandler(Command, info))
            throw new InvalidOperationException(UiText.Get(TextKey.CommandConflict, Command));
        shortCommandRegistered = commandManager.AddHandler(ShortCommand, new CommandInfo(OnCommand) { HelpMessage = UiText.Get(TextKey.CommandHelp), ShowInHelp = false });
        if (!shortCommandRegistered) log.Warning("{Command} is used by another plugin; use {Long}.", ShortCommand, Command);

        pluginInterface.UiBuilder.Draw += windows.Draw;
        pluginInterface.UiBuilder.OpenMainUi += ToggleMain;
    }

    private static RelicCatalog LoadBundledCatalog()
    {
        using var stream = typeof(Plugin).Assembly.GetManifestResourceStream("DuckbillRelic.RelicCatalog")
            ?? throw new InvalidOperationException("Bundled relic catalog is missing.");
        using var reader = new StreamReader(stream);
        return RelicCatalog.Parse(reader.ReadToEnd());
    }

    private void OnCommand(string command, string arguments) => ToggleMain();
    private void ToggleMain() => mainWindow.Toggle();

    public void Dispose()
    {
        pluginInterface.UiBuilder.Draw -= windows.Draw;
        pluginInterface.UiBuilder.OpenMainUi -= ToggleMain;
        commandManager.RemoveHandler(Command);
        if (shortCommandRegistered) commandManager.RemoveHandler(ShortCommand);
        windows.RemoveAllWindows();
        observer.Dispose();
    }
}
