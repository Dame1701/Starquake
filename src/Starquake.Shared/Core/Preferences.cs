// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Core;

/// <summary>
///     The settings the function keys change, kept between runs: F9's cathode ray tube, F8's
///     artwork, F10's backdrops and F11's rate. See <see cref="PreferencesStore" />.
/// </summary>
/// <param name="CrtMode">How much of a cathode ray tube the picture goes through.</param>
/// <param name="OriginalGraphics">Whether the original 1985 graphics are in play.</param>
/// <param name="Backdrops">Whether the backdrops behind the tile layers are drawn.</param>
/// <param name="Hertz">How many logic frames a second the game runs at.</param>
internal sealed record Preferences(CrtMode CrtMode, bool OriginalGraphics, bool Backdrops, int Hertz)
{
    /// <summary>How the game starts with nothing saved, which is how it has always started.</summary>
    public static Preferences Defaults { get; } = new(CrtMode.Scanlines, false, true, LogicRate.Pal);
}
