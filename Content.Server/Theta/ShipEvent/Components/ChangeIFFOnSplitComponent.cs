using Content.Shared.Shuttles.Components;

namespace Content.Server.Theta.ShipEvent.Components;

/// <summary>
/// Changes flags & color on child grid IFF after parent grid was split
/// </summary>
[RegisterComponent]
public sealed class ChangeIFFOnSplitComponent : Component
{
    /// <summary>
    /// Will set new flags for children IFF, if not null, otherwise will inherit them from parent grid if possible
    /// </summary>
    [DataField("flags")]
    public IFFFlags? NewFlags;

    /// <summary>
    /// Will set a new color for children IFF, if not null, otherwise will inherit it from parent grid if possible
    /// </summary>
    [DataField("color")]
    public Color? NewColor;

    /// <summary>
    /// If set, children won't get an IFF component at all
    /// </summary>
    [DataField("remove")]
    public bool Remove;

    /// <summary>
    /// If set, component will be replicated to children
    /// </summary>
    [DataField("replicate")]
    public bool Replicate;

    /// <summary>
    /// If set, all child grids will be deleted when parent grid deleted
    /// In seconds.
    /// </summary>
    [DataField("deleteInheritedGridsDelay")]
    public float DeleteInheritedGridsDelay;
}
