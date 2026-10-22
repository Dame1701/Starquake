// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.Core;

/// <summary>What one frame of the planet's tremor came to.</summary>
/// <param name="XOffset">How far the picture is pushed sideways this frame.</param>
/// <param name="YOffset">How far it is pushed down.</param>
/// <param name="Rumbling">
///     Whether the ground is moving, which the rumble sound follows. True on every frame of a tremor
///     rather than only the first, because the sound is asked for repeatedly and the sound engine
///     keeps one instance of it.
/// </param>
internal readonly record struct Shake(int XOffset, int YOffset, bool Rumbling)
{
    /// <summary>The ground is not moving: no offset, no rumble.</summary>
    public static Shake Still => new(0, 0, false);

    /// <summary>
    ///     Whether the view port actually moves. Both offsets are rolled from nought, so a tremor has
    ///     frames where one comes up nought and the picture stays put, which is part of what makes it
    ///     read as uneven rather than a buzz.
    /// </summary>
    public bool MovesTheViewPort => XOffset > 0 && YOffset > 0;
}
