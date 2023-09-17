using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Mind.Components
{
    /// <summary>
    ///     Stores a <see cref="MindComponent"/> on a mob.
    /// </summary>
    [RegisterComponent, Access(typeof(SharedMindSystem))]
    public sealed partial class MindContainerComponent : Component
    {
        /// <summary>
        ///     The mind controlling this mob. Can be null.
        /// </summary>
        [ViewVariables]
        [Access(typeof(SharedMindSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
        public EntityUid? Mind { get; set; }

        /// <summary>
        ///     True if we have a mind, false otherwise.
        /// </summary>
        [ViewVariables]
        [MemberNotNullWhen(true, nameof(Mind))]
        public bool HasMind => Mind != null;

        /// <summary>
        ///     Whether examining should show information about the mind or not.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("showExamineInfo")]
        public bool ShowExamineInfo { get; set; }

        /// <summary>
        ///     Whether the mind will be put on a ghost after this component is shutdown.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("ghostOnShutdown")]
        [Access(typeof(SharedMindSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
        public bool GhostOnShutdown { get; set; } = true;

        /// <summary>
        ///     Ghost type which will be spawned when this component is shutting down. Also requires ghostOnShutdown.
        /// </summary>
        [DataField("ghostPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string GhostPrototype = "MobObserver";
    }

    public sealed class MindRemovedMessage : EntityEventArgs
    {
        public EntityUid OldMindId;
        public MindComponent OldMind;

        public MindRemovedMessage(EntityUid oldMindId, MindComponent oldMind)
        {
            OldMindId = oldMindId;
            OldMind = oldMind;
        }
    }

    public sealed class MindAddedMessage : EntityEventArgs
    {
    }

    public sealed class MindTransferredMessage : EntityEventArgs
    {
        public EntityUid? OldEntity;
        public EntityUid? NewEntity;
        public MindComponent Mind;
        public MindContainerComponent? NewComponent;

        public MindTransferredMessage(EntityUid? oldEnt, EntityUid? newEnt, MindComponent mind, MindContainerComponent? newComp)
        {
            NewEntity = newEnt;
            OldEntity = oldEnt;
            Mind = mind;
            NewComponent = newComp;
        }
    }
}
