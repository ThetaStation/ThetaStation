using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared.Roles.Theta;
using System.Linq;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed partial class ShipEventTeamSystem : EntitySystem
{
    public float ModifierUpdateInterval;
    public int ModifierAmount;
    public List<ShipEventModifierPrototype> AllModifiers = new();
    public List<ShipEventModifierPrototype> ActiveModifiers = new();

    private void InitializeModifiers()
    {
        ModifierUpdate();
    }

    private void ModifierUpdate()
    {
        List<ShipEventModifierPrototype> modifiersListCopy = new(AllModifiers);

        DisableAllModifiers();

        int i = 0;
        while (i < ModifierAmount)
        {
            if (modifiersListCopy.Count == 0)
            {
                Log.Error("Tried to add more modifiers than the available amount");
                return;
            }

            ShipEventModifierPrototype modifierProt = _random.PickAndTake(modifiersListCopy);

            bool allEligible = true;
            foreach (var modifier in modifierProt.Modifiers)
            {
                if (!modifier.CheckEligibility())
                    allEligible = false;
            }

            if (allEligible)
            {
                foreach (var modifier in modifierProt.Modifiers)
                {
                    modifier.OnApply();
                    ActiveModifiers.Add(modifierProt);
                    i++;
                }
            }
        }

        foreach (ShipEventTeam team in Teams)
        {
            TeamMessage(team,
                Loc.GetString("se-modifier-updated", ("time", ModifierUpdateInterval / 60)) + "\n" + string.Join("\n", ActiveModifiers.Select(m => Loc.GetString(m.Name))),
                color: Color.LightGray);
        }
    }

    private void DisableAllModifiers()
    {
        foreach (var modifierProt in ActiveModifiers)
        {
            foreach (var modifier in modifierProt.Modifiers)
            {
                modifier.OnRemove();
            }
        }
        ActiveModifiers.Clear();
    }
}

[Prototype("modifier")]
public sealed class ShipEventModifierPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = "";

    [DataField("name", required: true)]
    public string Name { get; } = "";

    [DataField("icon", required: true)]
    public SpriteSpecifier Icon = SpriteSpecifier.Invalid;

    [DataField("modifiers", required: true)]
    public List<ShipEventModifier> Modifiers = default!;
}

[ImplicitDataDefinitionForInheritors]
public abstract partial class ShipEventModifier
{
    public virtual bool CheckEligibility() { return true; }

    public virtual void OnApply() { }
    public virtual void OnRemove() { }
}