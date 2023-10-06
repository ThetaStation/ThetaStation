using Content.Server.Theta.ShipEvent.Components;

namespace Content.Server.Theta.ShipEvent.Systems;

public partial class ShipEventFactionSystem
{
    private float hideTimer;
    private float hideCooldownTimer;
    private bool startHideTimer;
    private bool startHideCooldownTimer;

    public bool CheckStealthTimer()
    {
        if (hideTimer > 100 || hideCooldownTimer < 100)
            return false;
        else
            return true;
    }

    public void StealthTimer()
    {
        if (hideCooldownTimer < 100 && startHideCooldownTimer)
        {
            hideCooldownTimer += 1;
            Log.Info("Идёт таймер кулдауна" + "     " + CheckStealthTimer().ToString());
        }
        else if (hideTimer < 100 && startHideTimer)
        {
            hideTimer += 1;
            Log.Info("Идёт таймер скрытия" + "     " + CheckStealthTimer().ToString());

        }
        else
        {
            Log.Info("Таймер кулдауна: " + hideCooldownTimer.ToString() + "     " + CheckStealthTimer().ToString());
            Log.Info("Таймер скрытия: " + hideTimer.ToString() + "     " + CheckStealthTimer().ToString());
        }
    }

    public void SetStealthState(bool boolState, EntityUid gridUid)
    {
        if (boolState)
        {
            startHideCooldownTimer = true;
            startHideTimer = false;
            hideTimer = 0;
            Log.Info("Начало кулдауна, значения: " + hideTimer.ToString() + "     " + hideCooldownTimer.ToString() + "     " + CheckStealthTimer().ToString());
        }
        else
        {
            startHideCooldownTimer = false;
            hideCooldownTimer = 0;
            startHideTimer = true;
            Log.Info("Начало скрытия, значения: " + hideTimer.ToString() + "     " + hideCooldownTimer.ToString() + "     " + CheckStealthTimer().ToString());
        }
    }
}
