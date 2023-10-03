using Content.Server.Shuttles.Components;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Events;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    private void InitializeIFF()
    {
        SubscribeLocalEvent<IFFConsoleComponent, AnchorStateChangedEvent>(OnIFFConsoleAnchor);
        SubscribeLocalEvent<IFFConsoleComponent, IFFShowIFFMessage>(OnIFFShow);
        SubscribeLocalEvent<IFFConsoleComponent, IFFShowVesselMessage>(OnIFFShowVessel);
    }

    private void OnIFFShow(EntityUid uid, IFFConsoleComponent component, IFFShowIFFMessage args)
    {
        if (!TryComp<TransformComponent>(uid, out var xform) || xform.GridUid == null ||
            (component.AllowedFlags & IFFFlags.HideLabel) == 0x0)
        {
            return;
        }

        if (!args.Show)
        {
            AddIFFFlag(xform.GridUid.Value, IFFFlags.HideLabel);
        }
        else
        {
            RemoveIFFFlag(xform.GridUid.Value, IFFFlags.HideLabel);
        }
    }

    private void OnIFFShowVessel(EntityUid uid, IFFConsoleComponent component, IFFShowVesselMessage args)
    {
        if (!TryComp<TransformComponent>(uid, out var xform) || xform.GridUid == null ||
        (component.AllowedFlags & IFFFlags.Hide) == 0x0)
        {
            return;
        }

        if (hideTimer > 10 && !startHideCooldownTimer && !args.Show) //если время нахождения в инвизе истекло И не идёт кулдаун И корабль не видно
        {
            Log.Info("Кулдаун: " + hideCooldownTimer.ToString() + " Заряд скрытия: " + hideTimer.ToString() + " Идёт ли таймер куладуна?: " + startHideCooldownTimer.ToString());
            startHideCooldownTimer = true; //Запускаем кулдаун
            startHideTimer = false; //Выключаем таймер нахождения в инвизе
            hideTimer = 0; //Сбрасываем таймер нахождения в инвизе
            RemoveIFFFlag(xform.GridUid.Value, IFFFlags.Hide); //вырубаем инвиз
            Log.Info("Закончился заряд скрытия");
        }
        else if (hideCooldownTimer > 10 || (hideCooldownTimer == 0 && !startHideCooldownTimer)) //иначе если кулдаун прошел ИЛИ кулдаун равен нулю и не был запущен
        {
            Log.Info("1_Кулдаун: " + hideCooldownTimer.ToString() + " Заряд скрытия: " + hideTimer.ToString() + " Идёт ли таймер куладуна?: " + startHideCooldownTimer.ToString());
            startHideCooldownTimer = false; //выключаем кулдаун
            hideCooldownTimer = 0; //сбрасываем кулдаун
            startHideTimer = true; //включаем таймер инвиза
            AddIFFFlag(xform.GridUid.Value, IFFFlags.Hide); //устанавливаем инвиз
        }




        /*f (!args.Show && canHide)
        {
            AddIFFFlag(xform.GridUid.Value, IFFFlags.Hide);
            startHideTimer = true;
        }
        else
        {
            RemoveIFFFlag(xform.GridUid.Value, IFFFlags.Hide);
            startHideTimer = false;
            startHideCooldownTimer = true;
            hideTimer = 0;
        }*/
    }

    private void OnIFFConsoleAnchor(EntityUid uid, IFFConsoleComponent component, ref AnchorStateChangedEvent args)
    {
        // If we anchor / re-anchor then make sure flags up to date.
        if (!args.Anchored ||
            !TryComp<TransformComponent>(uid, out var xform) ||
            !TryComp<IFFComponent>(xform.GridUid, out var iff))
        {
            _uiSystem.TrySetUiState(uid, IFFConsoleUiKey.Key, new IFFConsoleBoundUserInterfaceState()
            {
                AllowedFlags = component.AllowedFlags,
                Flags = IFFFlags.None,
            });
        }
        else
        {
            _uiSystem.TrySetUiState(uid, IFFConsoleUiKey.Key, new IFFConsoleBoundUserInterfaceState()
            {
                AllowedFlags = component.AllowedFlags,
                Flags = iff.Flags,
            });
        }
    }

    protected override void UpdateIFFInterfaces(EntityUid gridUid, IFFComponent component)
    {
        base.UpdateIFFInterfaces(gridUid, component);
        foreach (var (comp, xform) in EntityQuery<IFFConsoleComponent, TransformComponent>(true))
        {
            if (xform.GridUid != gridUid)
                continue;

            _uiSystem.TrySetUiState(comp.Owner, IFFConsoleUiKey.Key, new IFFConsoleBoundUserInterfaceState()
            {
                AllowedFlags = comp.AllowedFlags,
                Flags = component.Flags,
            });
        }
    }
}
