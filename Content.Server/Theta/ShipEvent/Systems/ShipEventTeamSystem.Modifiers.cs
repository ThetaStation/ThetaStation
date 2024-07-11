using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed partial class ShipEventTeamSystem : EntitySystem
{
    public float ModifierUpdateInterval;
    public int ModifierAmount;
    public List<ShipEventModifierPrototype> AllModifiers = new();
    public List<ShipEventModifierPrototype> ActiveModifiers = new();

    private void ModifierUpdate()
    {
        List<ShipEventModifierPrototype> modifiersListCopy = new(AllModifiers);

        foreach (ShipEventModifierPrototype prot in ActiveModifiers)
        {
            prot.Modifier.OnRemove();
        }
        ActiveModifiers.Clear();

        int i = 0;
        while (i < ModifierAmount)
        {
            if (modifiersListCopy.Count == 0)
            {
                Log.Error("Tried to add more modifiers than the available amount");
                return;
            }

            ShipEventModifierPrototype modifier = _random.PickAndTake(modifiersListCopy);
            if (modifier.Modifier.CheckEligibility())
            {
                modifier.Modifier.OnApply();
                ActiveModifiers.Add(modifier);
                i++;
            }
        }
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

    [DataField("modifier", required: true)]
    public ShipEventModifier Modifier = default!;
}

[ImplicitDataDefinitionForInheritors]
public abstract partial class ShipEventModifier
{
    public virtual bool CheckEligibility() { return true; }

    public virtual void OnApply() { }
    public virtual void OnRemove() { }
}