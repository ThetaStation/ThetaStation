using System.Timers;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class TestAudioParamsChange : IConsoleCommand
{
    public string Command => "test_ap_change";
    public string Description => "Test audio parameters change.";
    public string Help => "No arguments required.";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var audioSys = IoCManager.Resolve<EntityManager>().System<AudioSystem>();
        var parameters = AudioParams.Default;
        parameters.Loop = true;
        parameters.Volume = -1f;
        
        var stream = audioSys.PlayGlobal("/Audio/Machines/alarm.ogg", Filter.Broadcast(), false, parameters);
        if (stream == null)
            return;
        shell.WriteLine("Started.");

        parameters.Volume = 5f;
        parameters.Loop = false;
        Timer.Spawn(3000, () =>
        {
            shell.WriteLine("Raising volume!");
            audioSys.SetAudioParams(stream, parameters);
        });
    }
}

