using Robust.Shared.Random;

namespace Content.Server.Theta.NiceColors.ColorPalettes;

public sealed class ShipEventPalette : ColorPalette
{
    [Dependency] protected readonly IRobustRandom _random = default!;

    private List<Color> _palette;
    public override List<Color> Palette => _palette;

    public ShipEventPalette()
    {
        _palette = GetPalette();
        _random.Shuffle(_palette);
    }

    // from https://lospec.com/palette-list/resurrect-64 and some my colors
    private List<Color> GetPalette()
    {
        return new List<Color>
        {
            Color.FromHex("#2e222f"),
            Color.FromHex("#3e3546"),
            Color.FromHex("#966c6c"),
            Color.FromHex("#625565"),
            Color.FromHex("#ffedc8"),
            Color.FromHex("#ab947a"),
            Color.FromHex("#694f62"),
            Color.FromHex("#7f708a"),
            Color.FromHex("#9babb2"),
            Color.FromHex("#c7dcd0"),
            Color.FromHex("#c6c0d7"),
            Color.FromHex("#ffffff"),
            Color.FromHex("#6e2727"),
            Color.FromHex("#b33831"),
            Color.FromHex("#ea4f36"),
            Color.FromHex("#f57d4a"),
            Color.FromHex("#87a647"),
            Color.FromHex("#ae2334"),
            Color.FromHex("#e83b3b"),
            Color.FromHex("#fb6b1d"),
            Color.FromHex("#f79617"),
            Color.FromHex("#f9c22b"),
            Color.FromHex("#7a3045"),
            Color.FromHex("#9e4539"),
            Color.FromHex("#cd683d"),
            Color.FromHex("#e6904e"),
            Color.FromHex("#fbb954"),
            Color.FromHex("#4c3e24"),
            Color.FromHex("#676633"),
            Color.FromHex("#6083b6"),
            Color.FromHex("#a2a947"),
            Color.FromHex("#d5e04b"),
            Color.FromHex("#fbff86"),
            Color.FromHex("#165a4c"),
            Color.FromHex("#239063"),
            Color.FromHex("#1ebc73"),
            Color.FromHex("#91db69"),
            Color.FromHex("#cddf6c"),
            Color.FromHex("#313638"),
            Color.FromHex("#374e4a"),
            Color.FromHex("#547e64"),
            Color.FromHex("#92a984"),
            Color.FromHex("#b2ba90"),
            Color.FromHex("#0b5e65"),
            Color.FromHex("#0b8a8f"),
            Color.FromHex("#0eaf9b"),
            Color.FromHex("#30e1b9"),
            Color.FromHex("#8ff8e2"),
            Color.FromHex("#323353"),
            Color.FromHex("#484a77"),
            Color.FromHex("#4d65b4"),
            Color.FromHex("#4d9be6"),
            Color.FromHex("#8fd3ff"),
            Color.FromHex("#45293f"),
            Color.FromHex("#6b3e75"),
            Color.FromHex("#905ea9"),
            Color.FromHex("#a884f3"),
            Color.FromHex("#eaaded"),
            Color.FromHex("#753c54"),
            Color.FromHex("#a24b6f"),
            Color.FromHex("#cf657f"),
            Color.FromHex("#ed8099"),
            Color.FromHex("#831c5d"),
            Color.FromHex("#c32454"),
            Color.FromHex("#f04f78"),
            Color.FromHex("#f68181"),
            Color.FromHex("#fca790"),
            Color.FromHex("#fdcbb0"),
        };
    }
}
