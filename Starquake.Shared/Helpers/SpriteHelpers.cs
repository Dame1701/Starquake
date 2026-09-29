// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Structure;

namespace Starquake.Helpers;

public class SpriteHelpers
{
    /// <summary>
    ///     Gets a collision offset point for particles based on the direction a sprite is moving, assumes sprite is drawn from
    ///     top left
    /// </summary>
    /// <param name="direction">Direction the sprite is moving in</param>
    /// <param name="SpriteLocation">The size of the sprite</param>
    /// <returns></returns>
    public static Point GetSpriteLocationOffset(Direction direction, int spriteWidth, int spriteHeight)
    {
        if (direction == Direction.UpRight)
        {
            return new Point(0, 0);
        }
        if (direction == Direction.Up)
        {
            return new Point(spriteWidth / 2, 0);
        }
        if (direction == Direction.UpRight)
        {
            return new Point(spriteWidth, 0);
        }
        if (direction == Direction.Left)
        {
            return new Point(0, spriteHeight / 2);
        }
        if (direction == Direction.DownLeft)
        {
            return new Point(0, spriteHeight);
        }
        if (direction == Direction.Down)
        {
            return new Point(spriteWidth / 2, spriteHeight);
        }
        if (direction == Direction.DownRight)
        {
            return new Point(spriteWidth, spriteHeight);
        }
        if (direction == Direction.Right)
        {
            return new Point(spriteWidth, spriteHeight / 2);
        }

        // no direction so return the centre of the sprite
        return new Point(spriteWidth / 2, spriteHeight / 2);
    }
}