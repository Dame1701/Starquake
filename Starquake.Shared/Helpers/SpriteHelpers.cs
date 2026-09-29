// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace StarquakeXNA;

public class SpriteHelpers
{
    /// <summary>
    ///     Gets a collision offset point for particles based on the direction a sprite is moving, assumes sprite is drawn from
    ///     top left
    /// </summary>
    /// <param name="Direction">Direction the sprite is moving in</param>
    /// <param name="SpriteLocation">The size of the sprite</param>
    /// <returns></returns>
    public static Point GetSpriteLocationOffset(enDirection Direction, int SpriteWidth, int SpriteHeight)
    {
        if (Direction == enDirection.UpRight)
        {
            return new Point(0, 0);
        }
        if (Direction == enDirection.Up)
        {
            return new Point(SpriteWidth / 2, 0);
        }
        if (Direction == enDirection.UpRight)
        {
            return new Point(SpriteWidth, 0);
        }
        if (Direction == enDirection.Left)
        {
            return new Point(0, SpriteHeight / 2);
        }
        if (Direction == enDirection.DownLeft)
        {
            return new Point(0, SpriteHeight);
        }
        if (Direction == enDirection.Down)
        {
            return new Point(SpriteWidth / 2, SpriteHeight);
        }
        if (Direction == enDirection.DownRight)
        {
            return new Point(SpriteWidth, SpriteHeight);
        }
        if (Direction == enDirection.Right)
        {
            return new Point(SpriteWidth, SpriteHeight / 2);
        }

        // no direction so return the centre of the sprite
        return new Point(SpriteWidth / 2, SpriteHeight / 2);
    }
}