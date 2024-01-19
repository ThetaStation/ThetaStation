using Robust.Shared.Prototypes;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public sealed partial class TriggerOnCollideComponent : Component
    {
		[DataField("fixtureID", required: true)]
		public string FixtureID = String.Empty;

        /// <summary>
        ///     Doesn't trigger if the other colliding fixture is nonhard.
        /// </summary>
        [DataField("ignoreOtherNonHard")]
        public bool IgnoreOtherNonHard = true;

        /// <summary>
        /// Components that should be present on the other fixture to trigger this one.
        /// </summary>
        [DataField("componentFilter")]
        public ComponentRegistry RequiredComponents = new();
    }
}
