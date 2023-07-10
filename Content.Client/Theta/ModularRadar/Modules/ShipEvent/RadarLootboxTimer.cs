using Content.Client.Theta.ShipEvent.Systems;
using Content.Shared.Theta.ShipEvent;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;

namespace Content.Client.Theta.ModularRadar.Modules.ShipEvent;

/// <summary>
/// Displays lifetime of lootboxes on field
/// </summary>
public sealed class RadarLootboxTimer : RadarModule
{
    private readonly IResourceCache resCache;
    private readonly LootboxInfoSystem lootboxInfoSys;
    private LootboxInfo? lootboxInfo;
    private Font font;
    private DateTime lastTime = DateTime.MinValue;

    public RadarLootboxTimer(ModularRadarControl parentRadar) : base(parentRadar)
    {
        resCache = IoCManager.Resolve<IResourceCache>();
        font = new VectorFont(resCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);

        lootboxInfoSys = EntManager.System<LootboxInfoSystem>();
        lootboxInfoSys.OnLootboxInfoReceived += UpdateLootboxInfo;
        lootboxInfoSys.RequestLootboxInfo();
    }

    public void UpdateLootboxInfo(LootboxInfo info)
    {
        lootboxInfo = info;
    }

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        base.Draw(handle, parameters);
        
        if (lastTime == DateTime.MinValue)
            lastTime = DateTime.Now;
        float dt = (float)(DateTime.Now - lastTime).TotalSeconds;
        lastTime = DateTime.Now;

        if (lootboxInfo == null)
            return;

        for(int i = 0; i < lootboxInfo.Entities.Count; i++)
        {
            Vector2 pos = lootboxInfo.Bounds[i].Center;
            
            pos = parameters.DrawMatrix.Transform(pos);
            pos.Y = -pos.Y;
            pos = ScalePosition(pos);
            
            handle.DrawString(font, pos, ((int)lootboxInfo.Lifetime[i]).ToString(), Color.Yellow);
            lootboxInfo.Lifetime[i] -= dt; //so we don't stop counting if we can't fetch new lootbox data rn
        }
    }
}
