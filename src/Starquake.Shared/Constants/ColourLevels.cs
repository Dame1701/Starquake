// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.Constants;

/// <summary>
///     The ends of the range a colour channel is measured on. Fades are counted in eight-bit channel
///     levels rather than floats, so nearly every fade counter runs between these two numbers.
/// </summary>
internal static class ColourLevels
{
    /// <summary>A fully lit channel, and fully opaque when it is the alpha channel.</summary>
    public const int Full = 255;

    /// <summary>An unlit channel, and fully transparent when it is the alpha channel.</summary>
    public const int None = 0;
}
