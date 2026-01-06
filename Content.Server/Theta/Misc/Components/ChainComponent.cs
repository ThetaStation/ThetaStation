namespace Content.Server.Theta.Misc.Components;

[RegisterComponent]
public sealed partial class ChainComponent : Component
{
    [DataField("boundUid"), ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? BoundUid;

    [DataField("freq"), ViewVariables(VVAccess.ReadWrite)]
    public float Frequency = 0.05f; //how wobbly joint is

    public float Damping = 5f; //how (un)sensitive joint is (in sense that small forces will get absorbed fast with high damping)
}