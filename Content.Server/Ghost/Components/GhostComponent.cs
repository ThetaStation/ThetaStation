using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Ghost;
using Robust.Shared.Utility;

namespace Content.Server.Ghost.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedGhostComponent))]
    public sealed class GhostComponent : SharedGhostComponent
    {
        public TimeSpan TimeOfDeath { get; set; } = TimeSpan.Zero;

        [DataField("booRadius")]
        public float BooRadius = 3;

        [DataField("booMaxTargets")]
        public int BooMaxTargets = 3;

        [DataField("actions")]
        public List<InstantAction>? Actions;
    }

    public sealed class BooActionEvent : InstantActionEvent { }
}
