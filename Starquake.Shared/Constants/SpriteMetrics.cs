// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Constants;

/// <summary>
///     The size of the things that move about on a screen. Nearly everything the game animates is
///     drawn from a sheet cut into frames of one standard size, so a sprite's width and height are
///     the same number almost everywhere.
/// </summary>
internal static class SpriteMetrics
{
    /// <summary>
    ///     The standard sprite frame. Every sheet holding a moving object is cut into frames of this
    ///     size, so it is the width and height of Blob, of a nasty, and of anything Blob can pick up.
    /// </summary>
    public const int SpriteWidth = 64;

    /// <inheritdoc cref="SpriteWidth" />
    public const int SpriteHeight = 64;

    /// <summary>
    ///     Half a sprite, for anything aimed at a sprite's middle rather than its corner: a particle
    ///     emitter, the point a shot leaves Blob from, the centre of a core element.
    /// </summary>
    public const int HalfSpriteWidth = SpriteWidth / 2;

    /// <inheritdoc cref="HalfSpriteWidth" />
    public const int HalfSpriteHeight = SpriteHeight / 2;
}
