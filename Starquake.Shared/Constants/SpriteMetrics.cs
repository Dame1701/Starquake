// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Constants;

/// <summary>
///     The size of the things that move about on a screen. Almost everything the game animates -
///     the blob, the nasties, the pickups, the power-ups, a core element - is drawn from a sheet
///     cut into frames of one standard size, so a sprite's width and height are the same number
///     nearly everywhere and worth saying once.
/// </summary>
internal static class SpriteMetrics
{
    /// <summary>
    ///     The standard sprite frame. Every sheet that holds a moving object is cut into frames of
    ///     this size, which makes it the width and height of the blob, of a nasty, and of anything
    ///     the blob can pick up.
    /// </summary>
    public const int SpriteWidth = 64;

    /// <inheritdoc cref="SpriteWidth" />
    public const int SpriteHeight = 64;

    /// <summary>
    ///     Half a sprite. Used wherever something is aimed at a sprite's middle rather than its
    ///     corner - a particle emitter, the point a shot leaves the blob from, the centre of a
    ///     core element.
    /// </summary>
    public const int HalfSpriteWidth = SpriteWidth / 2;

    /// <inheritdoc cref="HalfSpriteWidth" />
    public const int HalfSpriteHeight = SpriteHeight / 2;
}
