// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.ActiveSprites;

/// <summary>
///     Which of its three courses a shot is on. It leaves straight up, down, left or right, and the
///     first thing it hits sends it off diagonally one way or the other rather than stopping it.
/// </summary>
/// <remarks>
///     The numbers are load-bearing. A deflection is chosen with one call to the shared random number
///     generator over the range <see cref="Left" /> to <see cref="Right" />, so those two must stay
///     adjacent and <see cref="None" /> must stay outside the range.
/// </remarks>
internal enum FireDeflection
{
    /// <summary>Still going the way it was fired.</summary>
    None = 0,

    /// <summary>Deflected towards the low end of the axis it was not fired along.</summary>
    Left = 1,

    /// <summary>Deflected towards the high end of that axis.</summary>
    Right = 2
}
