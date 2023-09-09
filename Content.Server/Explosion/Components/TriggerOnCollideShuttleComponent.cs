namespace Content.Server.Explosion.Components;

[RegisterComponent]
public sealed class TriggerOnCollideShuttleComponent : Component
{
    [DataField("fixtureID", required: true)]
    public string FixtureID = String.Empty;
}
