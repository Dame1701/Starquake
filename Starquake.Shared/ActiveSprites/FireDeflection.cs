// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.ActiveSprites;

/// <summary>
///     What a shot is doing. It leaves straight up, down, left or right, and the first thing it
///     hits sends it off diagonally one way or the other rather than stopping it, so this says
///     which of the three it is currently on.
/// </summary>
/// <remarks>
///     The numbers are load-bearing: a deflection is chosen with one call to the shared random
///     number generator over the range <see cref="Left" /> to <see cref="Right" />, so they have
///     to stay adjacent and <see cref="None" /> has to stay outside that range.
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
