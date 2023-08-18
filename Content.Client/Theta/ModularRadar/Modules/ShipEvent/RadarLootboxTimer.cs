using System.Numerics;
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
    [Dependency] private readonly IResourceCache _resCache = default!;
    private readonly LootboxInfoSystem _lootboxInfoSys;
    private LootboxInfo? _lootboxInfo;
    
    private Font _font;
    private const int OffsetX = 0;
    private const int OffsetY = -6;
    
    private DateTime lastTime = DateTime.MinValue;

    public RadarLootboxTimer(ModularRadarControl parentRadar) : base(parentRadar)
    {
        _font = new VectorFont(_resCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);

        _lootboxInfoSys = EntManager.System<LootboxInfoSystem>();
        _lootboxInfoSys.OnLootboxInfoReceived += UpdateLootboxInfo;
        _lootboxInfoSys.RequestLootboxInfo();
    }

    public void UpdateLootboxInfo(LootboxInfo info)
    {
        _lootboxInfo = info;
    }

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        base.Draw(handle, parameters);
        
        if (lastTime == DateTime.MinValue)
            lastTime = DateTime.Now;
        float dt = (float)(DateTime.Now - lastTime).TotalSeconds;
        lastTime = DateTime.Now;

        if (_lootboxInfo == null)
            return;
        
        for(int i = 0; i < _lootboxInfo.Entities.Count; i++)
        {
            Vector2 pos = _lootboxInfo.Bounds[i].Center;
            
            pos = parameters.DrawMatrix.Transform(pos);
            pos.Y = -pos.Y;
            pos = ScalePosition(pos);
            pos.X += OffsetX;
            pos.Y += OffsetY;
            
            int minutes = (int)Math.Floor(_lootboxInfo.Lifetime[i] / 60);
            int seconds = (int)_lootboxInfo.Lifetime[i] - minutes*60;
            
            handle.DrawString(_font, pos, $"{minutes:D2}:{seconds:D2}", Color.HotPink);
            _lootboxInfo.Lifetime[i] -= dt; //so we don't stop counting if we can't fetch new lootbox data rn
        }
    }
}
