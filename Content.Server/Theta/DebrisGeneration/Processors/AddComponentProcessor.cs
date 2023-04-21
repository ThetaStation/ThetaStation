using JetBrains.Annotations;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Theta.DebrisGeneration.Processors;

/// <summary>
/// Processor which adds specified components onto processed grid (not grid entities)
/// </summary>
[UsedImplicitly]
public sealed class AddComponentsProcessor : Processor
{
    [DataField("components", required: true)]
    [AlwaysPushInheritance]
    public EntityPrototype.ComponentRegistry Components = new();
    
    public override void Process(DebrisGenerationSystem sys, EntityUid gridUid, bool isGlobal)
    {
        if (isGlobal)
        {
            foreach (var childGridUid in sys.SpawnedGrids)
            {
                AddComponents(sys, childGridUid);
            }
        }
        else
        {
            AddComponents(sys, gridUid);
        }
    }
    
    public void AddComponents(DebrisGenerationSystem sys, EntityUid gridUid)
    {
        //todo: this is a copypaste from AddComponentSpecial, all concerns from here apply there too
        var factory = IoCManager.Resolve<IComponentFactory>();
        var serializationManager = IoCManager.Resolve<ISerializationManager>();

        foreach (var (name, data) in Components)
        {
            var component = (Component) factory.GetComponent(name);
            component.Owner = gridUid;

            var temp = (object) component;
            serializationManager.CopyTo(data.Component, ref temp);
            sys.EntMan.AddComponent(gridUid, (Component)temp!);
        }
    }
}
