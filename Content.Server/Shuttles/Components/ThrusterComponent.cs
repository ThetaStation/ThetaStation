using System.Numerics;
using Content.Server.Shuttles.Systems;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Shuttles.Components
{
    [RegisterComponent, NetworkedComponent]
    [Access(typeof(ThrusterSystem))]
    public sealed partial class ThrusterComponent : Component
    {
        /// <summary>
        /// Whether the thruster has been force to be enabled / disabled (e.g. VV, interaction, etc.)
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// This determines whether the thruster is actually enabled for the purposes of thrust
        /// </summary>
        public bool IsOn;

        // Need to serialize this because RefreshParts isn't called on Init and this will break post-mapinit maps!
        [ViewVariables(VVAccess.ReadWrite), DataField("thrust")]
        public float Thrust = 100f;

        [DataField("thrusterType")]
        public ThrusterType Type = ThrusterType.Linear;

        [DataField("burnShape")]
        public List<Vector2> BurnPoly = new()
        {
            new Vector2(-0.4f, 0.5f),
            new Vector2(-0.1f, 1.2f),
            new Vector2(0.1f, 1.2f),
            new Vector2(0.4f, 0.5f)
        };

        /// <summary>
        /// How much damage is done per second to anything colliding with our thrust.
        /// </summary>
        [DataField] public DamageSpecifier? Damage = new();

        [DataField]
        public bool RequireSpace = true;

        // Used for burns
        public List<EntityUid> Colliding = new();

        /// <summary>
        /// Use SetThrusterFiring instead of setting this manually
        /// </summary>
        public bool Firing = false;

        public TimeSpan LastFire;

        /// <summary>
        /// Next time we tick damage for anyone colliding.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField(customTypeSerializer:typeof(TimeOffsetSerializer))]
        public TimeSpan NextFire;

        [DataField("partRatingThrustMultiplier")]
        public float PartRatingThrustMultiplier = 1.5f;


        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public int LoadIdle;

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public int LoadFiring;

        /// <summary>
        /// How much time is required to go from idle consumption to max
        /// Thrust is not changed tho, because I'm lazy
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan RampDuration;

        public TimeSpan RampPosition;


        [DataField]
        public SoundSpecifier? SoundSpinup;

        [DataField]
        public SoundSpecifier? SoundCycle;

        [DataField]
        public SoundSpecifier? SoundShutdown;

        public EntityUid? AudioUid;
    }

    public enum ThrusterType
    {
        Linear,
        // Angular meaning rotational.
        Angular,
    }
}
