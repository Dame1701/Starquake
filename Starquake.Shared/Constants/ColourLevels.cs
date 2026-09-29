// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Constants;

/// <summary>
///     The ends of the range a colour channel is measured on. Fades throughout the game are counted
///     in eight-bit channel levels rather than in floats, so nearly every fade counter runs between
///     these two numbers and nearly every draw call tints with them.
/// </summary>
public static class ColourLevels
{
    /// <summary>
    ///     A fully lit channel, and so also fully opaque when it is the alpha channel being set.
    /// </summary>
    public const int Full = 255;

    /// <summary>
    ///     An unlit channel, and so also fully transparent when it is the alpha channel being set.
    /// </summary>
    public const int None = 0;
}
