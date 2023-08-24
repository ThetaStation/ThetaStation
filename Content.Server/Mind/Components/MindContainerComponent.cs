using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Mind.Components
{
    /// <summary>
    ///     Stores a <see cref="Server.Mind.Mind"/> on a mob.
    /// </summary>
    [RegisterComponent, Access(typeof(MindSystem))]
    public sealed class MindContainerComponent : Component
    {
        /// <summary>
        ///     The mind controlling this mob. Can be null.
        /// </summary>
        [ViewVariables]
        [Access(typeof(MindSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
        public Mind? Mind { get; set; }

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
        [Access(typeof(MindSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
        public bool GhostOnShutdown { get; set; } = true;

        /// <summary>
        ///     Ghost type which will be spawned when this component is shutting down. Also requires ghostOnShutdown.
        /// </summary>
        [DataField("ghostPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string GhostPrototype = "MobObserver";
    }

    public sealed class MindRemovedMessage : EntityEventArgs
    {
        public Mind OldMind;

        public MindRemovedMessage(Mind oldMind)
        {
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
        public Mind Mind;
        public MindContainerComponent? NewComponent;

        public MindTransferredMessage(EntityUid? oldEnt, EntityUid? newEnt, Mind mind, MindContainerComponent? newComp)
        {
            NewEntity = newEnt;
            OldEntity = oldEnt;
            Mind = mind;
            NewComponent = newComp;
        }
    }
}
