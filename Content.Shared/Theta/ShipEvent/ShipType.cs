using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Shared.Theta.ShipEvent;

[Serializable, NetSerializable]
[ImplicitDataDefinitionForInheritors]
public sealed class ShipType
{
    [DataField("name", required: true)]
    public string Name = "";

    [DataField("description", required: true)]
    public string Description = "";

    [DataField("previewImage")]
    public string PreviewImage = "";

    [DataField("class")]
    public ShipClass Class = ShipClass.Light;
    
    [DataField("structurePrototype", required: true)]
    public string StructurePrototype = "";

    [DataField("minCrewAmount")]
    public int MinCrewAmount = 1;
}

public enum ShipClass
{
    Light, //fast, maneuverable ships with little protection
    Medium, //something in between
    Heavy, //slow, sturdy and powerful ships
    Special
}
