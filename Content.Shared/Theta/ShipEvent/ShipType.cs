using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent;

[Serializable, NetSerializable]
[Prototype("shiptype")]
public sealed class ShipTypePrototype : IPrototype
{
    [IdDataField] 
    public string ID { get; } = default!;
    
    [DataField("name", required: true)]
    public string Name = "";

    [DataField("description", required: true)]
    public string Description = "";

    [DataField("previewImage")]
    public string PreviewImagePath = "";

    [DataField("class")]
    public string Class = "Light";
    
    [DataField("structurePrototype", required: true)]
    public string StructurePrototype = "";
    
    [DataField("minCrewAmount")]
    public int MinCrewAmount = 1; //This should be a recommendation, not a restraint
}
