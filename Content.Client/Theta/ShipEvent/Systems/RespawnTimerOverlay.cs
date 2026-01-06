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

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;
    private Font _font;
    public Vector2 _position;
    public TimeSpan TimeCountdown;

    public RespawnTimerOverlay()
    {
        IoCManager.InjectDependencies(this);
        _font = _cache.GetFont("/Fonts/NotoSans/NotoSans-Bold.ttf", 25);
    }

    public void Reset()
    {
        TimeCountdown = TimeSpan.Zero;
        _position = Vector2.Zero;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        TimeSpan delta = TimeSpan.FromSeconds(args.DeltaSeconds);
        TimeCountdown -= delta;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (TimeCountdown <= TimeSpan.Zero)
        {
            Reset();
            return false;
        }
        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        string text = GenerateText();

        if (_position == Vector2.Zero)
            _position = CalcPosition(_font, text, args.ViewportBounds.Width);

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
        return Loc.GetString("shipevent-respawntimerhud") + " " + TimeCountdown.Minutes.ToString("D2") + ":" + TimeCountdown.Seconds.ToString("D2");
    }
}
