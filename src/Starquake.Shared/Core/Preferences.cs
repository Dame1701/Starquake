// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.Core;

/// <summary>
///     The settings the function keys change, kept between runs: F9's cathode ray tube, F8's
///     artwork, F10's backdrops, F11's rate and F3's analogue walk. See <see cref="PreferencesStore" />.
/// </summary>
/// <param name="CrtMode">How much of a cathode ray tube the picture goes through.</param>
/// <param name="OriginalGraphics">Whether the original 1985 graphics are in play.</param>
/// <param name="Backdrops">Whether the backdrops behind the tile layers are drawn.</param>
/// <param name="Hertz">How many logic frames a second the game runs at.</param>
/// <param name="AnalogueWalk">Whether the left stick's lean sets how fast Blob walks.</param>
internal sealed record Preferences(
    CrtMode CrtMode,
    bool OriginalGraphics,
    bool Backdrops,
    int Hertz,
    bool AnalogueWalk)
{
    /// <summary>How the game starts with nothing saved, which is how it has always started.</summary>
    public static Preferences Defaults { get; } = new(CrtMode.Scanlines, false, true, LogicRate.Pal, false);
}
