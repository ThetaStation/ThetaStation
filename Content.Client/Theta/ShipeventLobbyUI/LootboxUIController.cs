using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Console;

namespace Content.Client.Theta.ShipeventLobbyUI;

[UsedImplicitly]
public sealed class LootboxUIController : UIController
{
    [Dependency] private readonly IConsoleHost _con = default!;

    public override void Initialize()
    {
        _con.RegisterCommand("lootbox", Loc.GetString("shipevent-cmd-lootbox-desc"), Loc.GetString("shipevent-cmd-lootbox-help"), LootboxCommand);
    }

    private void LootboxCommand(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            ToggleWindow();
            return;
        }
        OpenWindow();

        if (!int.TryParse(args[0], out var tab))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-int", ("arg", args[0])));
            return;
        }

        _lootboxWindow.Tabs.CurrentTab = tab;
    }

    private LootboxMenu _lootboxWindow = default!;

    private void EnsureWindow()
    {
        if (_lootboxWindow is { Disposed: false })
            return;

        _lootboxWindow = UIManager.CreateWindow<LootboxMenu>();
    }

    public void OpenWindow()
    {
        EnsureWindow();

        _lootboxWindow.OpenCentered();
        _lootboxWindow.MoveToFront();
    }

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_lootboxWindow.IsOpen)
        {
            _lootboxWindow.Close();
        }
        else
        {
            OpenWindow();
        }
    }
}
