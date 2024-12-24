using System.Numerics;
using System.Text;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Client.Theta.ShipEvent.Systems;

public sealed class RespawnTimerOverlay : Overlay
{
    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;
    private Font _font;
    public Vector2 _position;
    private TimeSpan _lastUpd = TimeSpan.Zero;
    public TimeSpan Time;

    public RespawnTimerOverlay()
    {
        IoCManager.InjectDependencies(this);
        _font = _cache.GetFont("/Fonts/NotoSans/NotoSans-Bold.ttf", 25);
    }

    public void Reset()
    {
        Time = TimeSpan.Zero;
        _position = Vector2.Zero;
        _lastUpd = TimeSpan.Zero;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (Time == TimeSpan.Zero)
            return;

        if (_lastUpd == TimeSpan.Zero)
        {
            _lastUpd = _timing.CurTime;
            return;
        }

        string text = GenerateText();

        if (_position == Vector2.Zero)
            _position = CalcPosition(_font, text, args.ViewportBounds.Width);

        Time -= _timing.CurTime - _lastUpd;
        if (Time <= TimeSpan.Zero)
        {
            Reset();
            return;
        }

        _lastUpd = _timing.CurTime;
        args.ScreenHandle.DrawString(_font, _position, text);
    }

    private Vector2 CalcPosition(Font font, string str, float vpWidth)
    {
        Vector2 strSize = new();
        foreach (Rune r in str)
        {
            if (font.TryGetCharMetrics(r, 1, out var metrics))
            {
                strSize.X += metrics.Width;
                strSize.Y = Math.Max(strSize.Y, metrics.Height);
            }
        }

        Vector2 pos = new Vector2((vpWidth - strSize.X) / 2, strSize.Y + 120);
        return pos;
    }

    private string GenerateText()
    {
        return Loc.GetString("shipevent-respawntimerhud") + " " + Time.Minutes.ToString("D2") + ":" + Time.Seconds.ToString("D2");
    }
}
