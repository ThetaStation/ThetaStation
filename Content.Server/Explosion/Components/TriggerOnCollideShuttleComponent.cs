namespace Content.Server.Explosion.Components;

[RegisterComponent]
public sealed partial class TriggerOnCollideShuttleComponent : Component
{
    [DataField("fixtureID", required: true)]
    public string FixtureID = String.Empty;
}
