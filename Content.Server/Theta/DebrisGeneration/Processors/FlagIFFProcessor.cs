using Content.Server.Shuttles.Systems;
using Content.Shared.Shuttles.Components;

namespace Content.Server.Theta.DebrisGeneration.Processors;

/// <summary>
/// Processor which adds specified IFF flags onto processed grid
/// </summary>
public sealed class FlagIFFProcessor : Processor
{
    [DataField("flags", required: true)]
    public List<IFFFlags> Flags = new();

    /// <summary>
    /// If set, will clear all old flags which were present on processed grid
    /// </summary>
    [DataField("resetOldFlags")]
    public bool ResetOldFlags;
    
    [DataField("colorOverride")]
    public Color? ColorOverride;
    
    public override void Process(DebrisGenerationSystem sys, EntityUid gridUid, bool isGlobal)
    {
        var shuttleSys = sys.EntMan.System<ShuttleSystem>();
        if (isGlobal)
        {
            foreach (var childGridUid in sys.SpawnedGrids)
            {
                ApplyFlags(sys.EntMan, shuttleSys, childGridUid);
            }
        }
        else
        {
            ApplyFlags(sys.EntMan, shuttleSys, gridUid);
        }
    }

    public void ApplyFlags(IEntityManager entMan, ShuttleSystem shuttleSys, EntityUid gridUid)
    {
        var iffComp = entMan.EnsureComponent<IFFComponent>(gridUid);
        
        if (ResetOldFlags)
            shuttleSys.ResetIFFFlags(gridUid, iffComp);
        
        if(ColorOverride != null)
            shuttleSys.SetIFFColor(gridUid, ColorOverride.Value, iffComp);

        foreach (IFFFlags flag in Flags) 
        {
            shuttleSys.AddIFFFlag(gridUid, flag, iffComp);
        }
    }
}