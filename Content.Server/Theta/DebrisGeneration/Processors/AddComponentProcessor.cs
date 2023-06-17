using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
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
    public ComponentRegistry Components = new();

    public override void Process(DebrisGenerationSystem sys, MapId targetMap, EntityUid gridUid, bool isGlobal)
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
        //todo: this is a copypaste from AddComponentSpecial, all concerns from there apply here too
        var factory = IoCManager.Resolve<IComponentFactory>();
        var serMan = IoCManager.Resolve<ISerializationManager>();
        var reflMan = IoCManager.Resolve<IReflectionManager>();

        foreach (var (name, data) in Components)
        {
            if (sys.EntMan.HasComponent(gridUid, reflMan.LooseGetType(name + "Component")))
            {
                Logger.Warning($"Add components processor, AddComponents: Tried to add {name} to {gridUid.ToString()}, which already possesses it.");
                continue;
            }
            
            var component = (Component) factory.GetComponent(name);
            component.Owner = gridUid;

            var temp = (object) component;
            serMan.CopyTo(data.Component, ref temp);
            sys.EntMan.AddComponent(gridUid, (Component)temp!);
        }
    }
}
