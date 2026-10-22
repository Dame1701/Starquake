// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.Core;

namespace Starquake.ActiveSprites;

/// <summary>
///     Geometry shared by anything that needs to place something against a sprite rather than draw it.
/// </summary>
internal static class SpriteOffsets
{
    /// <summary>
    ///     Where to emit particles from, given the direction a sprite is moving. Assumes the sprite is
    ///     drawn from its top left.
    /// </summary>
    /// <param name="direction">The direction the sprite is moving in.</param>
    /// <param name="spriteWidth">The sprite's width in pixels.</param>
    /// <param name="spriteHeight">The sprite's height in pixels.</param>
    /// <returns>
    ///     The offset from the sprite's top left to the edge or corner it leads with, or its centre
    ///     for <see cref="Direction.None" />.
    /// </returns>
    public static Point GetSpriteLocationOffset(Direction direction, int spriteWidth, int spriteHeight)
    {
        return direction switch
        {
            Direction.UpLeft => new Point(0, 0),
            Direction.Up => new Point(spriteWidth / 2, 0),
            Direction.UpRight => new Point(spriteWidth, 0),
            Direction.Left => new Point(0, spriteHeight / 2),
            Direction.DownLeft => new Point(0, spriteHeight),
            Direction.Down => new Point(spriteWidth / 2, spriteHeight),
            Direction.DownRight => new Point(spriteWidth, spriteHeight),
            Direction.Right => new Point(spriteWidth, spriteHeight / 2),
            _ => new Point(spriteWidth / 2, spriteHeight / 2)
        };

        // No direction, so the middle of the sprite.
    }
}
