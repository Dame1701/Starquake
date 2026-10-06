// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Core;

/// <summary>
///     What one frame of the planet's tremor came to.
/// </summary>
/// <param name="XOffset">How far the picture is pushed sideways this frame.</param>
/// <param name="YOffset">How far it is pushed down.</param>
/// <param name="Rumbling">
///     Whether the ground is moving, which is what the rumble sound follows. True on every frame of a
///     tremor rather than only its first, because the sound is asked for repeatedly and the sound engine
///     keeps one instance of it.
/// </param>
internal readonly record struct Shake(int XOffset, int YOffset, bool Rumbling)
{
    /// <summary>
    ///     The ground is not moving: no offset, and no rumble.
    /// </summary>
    public static Shake Still => new(0, 0, false);

    /// <summary>
    ///     Whether the view port is actually moved. Both offsets are rolled from nought, so a tremor has
    ///     frames where one of them comes up nought and the picture is left where it is - which is part of
    ///     what makes the tremor read as uneven rather than as a buzz.
    /// </summary>
    public bool MovesTheViewPort => XOffset > 0 && YOffset > 0;
}
