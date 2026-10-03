// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Structure;

namespace Starquake.Collisions;

internal static class CollisionDetection
{
    /// <inheritdoc cref="CheckDirection(Direction,int,int,int,int,int,int,int,int,int)" />
    private static int CheckDirection(Direction direction, int boundingBox1Velocity,
        Rectangle boundingBox1, Rectangle boundingBox2)
        => CheckDirection(
            direction,
            boundingBox1Velocity,
            boundingBox1.X,
            boundingBox1.Y,
            boundingBox1.Width,
            boundingBox1.Height,
            boundingBox2.X,
            boundingBox2.Y,
            boundingBox2.Width,
            boundingBox2.Height);

    /// <summary>
    ///     Checks for a collision between two objects and returns the amount of distance that it is legal to travel
    /// </summary>
    public static int CheckDirection(Direction direction, int obj1Velocity,
        int obj1X, int obj1Y, int obj1W, int obj1H,
        int obj2X, int obj2Y, int obj2W, int obj2H)
    {
        (int stepX, int stepY) = StepFor(direction);

        if (stepX == 0 && stepY == 0)
        {
            return 0;
        }

        if (obj1Velocity > 0 &&
            CannotMeet(stepX, stepY, obj1Velocity, obj1X, obj1Y, obj1W, obj1H, obj2X, obj2Y, obj2W, obj2H))
        {
            return obj1Velocity;
        }

        int amountThatCanBeMoved = 0;

        // Count up in steps to the full velocity requested to see if any collision is taking place
        for (int a = 1; a <= obj1Velocity; a++)
        {
            if (Check(obj1X + stepX * a, obj1Y + stepY * a, obj1W, obj1H, obj2X, obj2Y, obj2W, obj2H))
            {
                break;
            }

            amountThatCanBeMoved++;
        }

        return amountThatCanBeMoved;
    }

    /// <summary>
    ///     Whether the two boxes stay apart for every step of the move, and so whether the walk below
    ///     can be skipped. The walk tests one pixel at a time up to the velocity, so a screen's worth
    ///     of cells costs cells times velocity - and nearly all of those cells are nowhere near the
    ///     thing that is moving.
    /// </summary>
    /// <remarks>
    ///     Exact rather than approximate, so skipping changes no answer. It asks the same question
    ///     <see cref="Check(int,int,int,int,int,int,int,int)" /> asks - centres closer than the two
    ///     half-widths - of the whole swept path at once:
    ///     <list type="bullet">
    ///         <item>
    ///             Across the move, nothing changes as the step is taken, so one comparison settles
    ///             every step of it.
    ///         </item>
    ///         <item>
    ///             Along it, the centre sweeps between its first step and its last, and the nearest
    ///             it ever comes to the other box is at one end of that run or inside it.
    ///         </item>
    ///     </list>
    ///     The halves are integer divisions, written exactly as <c>Check</c> writes them, so the two
    ///     agree on the boundary rather than drifting a pixel apart.
    /// </remarks>
    private static bool CannotMeet(int stepX, int stepY, int obj1Velocity,
        int obj1X, int obj1Y, int obj1W, int obj1H,
        int obj2X, int obj2Y, int obj2W, int obj2H)
    {
        bool movingSideways = stepX != 0;

        int acrossTheMove = movingSideways
            ? Math.Abs(obj1Y + (obj1H / 2) - (obj2Y + (obj2H / 2)))
            : Math.Abs(obj1X + (obj1W / 2) - (obj2X + (obj2W / 2)));

        int acrossReach = movingSideways
            ? (obj1H / 2) + (obj2H / 2)
            : (obj1W / 2) + (obj2W / 2);

        if (acrossTheMove >= acrossReach)
        {
            return true;
        }

        int step = movingSideways ? stepX : stepY;
        int centre = movingSideways ? obj1X + (obj1W / 2) : obj1Y + (obj1H / 2);
        int other = movingSideways ? obj2X + (obj2W / 2) : obj2Y + (obj2H / 2);
        int alongReach = movingSideways ? (obj1W / 2) + (obj2W / 2) : (obj1H / 2) + (obj2H / 2);

        int firstStep = centre + step;
        int lastStep = centre + (step * obj1Velocity);

        return other <= Math.Min(firstStep, lastStep) - alongReach ||
               other >= Math.Max(firstStep, lastStep) + alongReach;
    }

    /// <summary>
    ///     Which way one step in a direction moves, or nought on both axes for a direction this does
    ///     not step in. Only the four straight directions do: a diagonal is checked one axis at a time
    ///     by the caller.
    /// </summary>
    private static (int X, int Y) StepFor(Direction direction)
        => direction switch
        {
            Direction.Up => (0, -1),
            Direction.Down => (0, 1),
            Direction.Left => (-1, 0),
            Direction.Right => (1, 0),
            _ => (0, 0)
        };

    /// <summary>
    ///     Which way a collision happened, or <see cref="Direction.None" /> if the first object can
    ///     travel its full velocity in every direction. A direction is blocked when no distance at
    ///     all is legal in it, and a blocked straight direction that is also blocked on one of the
    ///     two axes beside it reads as the diagonal between them.
    /// </summary>
    public static Direction CheckCollisionDirection(int obj1Velocity,
        int obj1X, int obj1Y, int obj1W, int obj1H,
        int obj2X, int obj2Y, int obj2W, int obj2H)
    {
        if (CheckDirection(Direction.Up, obj1Velocity, new Rectangle(obj1X, obj1Y, obj1W, obj1H), new Rectangle(obj2X, obj2Y, obj2W, obj2H)) == 0)
        {
            if (CheckDirection(Direction.Left, obj1Velocity, new Rectangle(obj1X, obj1Y, obj1W, obj1H), new Rectangle(obj2X, obj2Y, obj2W, obj2H)) == 0)
            {
                return Direction.UpLeft;
            }
            if (CheckDirection(Direction.Right, obj1Velocity, new Rectangle(obj1X, obj1Y, obj1W, obj1H), new Rectangle(obj2X, obj2Y, obj2W, obj2H)) == 0)
            {
                return Direction.UpRight;
            }
            return Direction.Up;
        }

        if (CheckDirection(Direction.Left, obj1Velocity, new Rectangle(obj1X, obj1Y, obj1W, obj1H), new Rectangle(obj2X, obj2Y, obj2W, obj2H)) == 0)
        {
            if (CheckDirection(Direction.Up, obj1Velocity, new Rectangle(obj1X, obj1Y, obj1W, obj1H), new Rectangle(obj2X, obj2Y, obj2W, obj2H)) == 0)
            {
                return Direction.UpLeft;
            }
            if (CheckDirection(Direction.Down, obj1Velocity, new Rectangle(obj1X, obj1Y, obj1W, obj1H), new Rectangle(obj2X, obj2Y, obj2W, obj2H)) == 0)
            {
                return Direction.DownLeft;
            }
            return Direction.Left;
        }

        if (CheckDirection(Direction.Down, obj1Velocity, new Rectangle(obj1X, obj1Y, obj1W, obj1H), new Rectangle(obj2X, obj2Y, obj2W, obj2H)) == 0)
        {
            if (CheckDirection(Direction.Left, obj1Velocity, new Rectangle(obj1X, obj1Y, obj1W, obj1H), new Rectangle(obj2X, obj2Y, obj2W, obj2H)) == 0)
            {
                return Direction.DownLeft;
            }
            if (CheckDirection(Direction.Right, obj1Velocity, new Rectangle(obj1X, obj1Y, obj1W, obj1H), new Rectangle(obj2X, obj2Y, obj2W, obj2H)) == 0)
            {
                return Direction.DownRight;
            }
            return Direction.Down;
        }

        if (CheckDirection(Direction.Right, obj1Velocity, new Rectangle(obj1X, obj1Y, obj1W, obj1H), new Rectangle(obj2X, obj2Y, obj2W, obj2H)) == 0)
        {
            if (CheckDirection(Direction.Up, obj1Velocity, new Rectangle(obj1X, obj1Y, obj1W, obj1H), new Rectangle(obj2X, obj2Y, obj2W, obj2H)) == 0)
            {
                return Direction.UpRight;
            }
            if (CheckDirection(Direction.Down, obj1Velocity, new Rectangle(obj1X, obj1Y, obj1W, obj1H), new Rectangle(obj2X, obj2Y, obj2W, obj2H)) == 0)
            {
                return Direction.DownRight;
            }
            return Direction.Right;
        }

        return Direction.None;
    }


    public static bool Check(Rectangle boundingBox1, Rectangle boundingBox2)
        => Check(boundingBox1.X, boundingBox1.Y, boundingBox1.Width, boundingBox1.Height,
            boundingBox2.X, boundingBox2.Y, boundingBox2.Width, boundingBox2.Height);

    /// <summary>
    ///     Whether two boxes overlap, by the distance between their centres.
    /// </summary>
    /// <remarks>
    ///     The eight working values here were eight private static fields until they were made
    ///     locals - the method is pure given its arguments, and they were only ever written and read
    ///     within one call, so two threads calling this at once silently read each other's
    ///     arithmetic. That is why test parallelisation was switched off.
    ///
    ///     <para>
    ///         The halves are integer division on purpose, so this is not Rectangle.Intersects and a
    ///         rewrite that reaches for the framework method changes how odd-width sprites collide.
    ///     </para>
    /// </remarks>
    public static bool Check(int obj1X, int obj1Y, int obj1W, int obj1H,
        int obj2X, int obj2Y, int obj2W, int obj2H)
    {
        // Centre point of each object
        int obj1CenterX = obj1X + obj1W / 2;
        int obj1CenterY = obj1Y + obj1H / 2;

        int obj2CenterX = obj2X + obj2W / 2;
        int obj2CenterY = obj2Y + obj2H / 2;

        // How far apart those centres are, on each axis
        int distanceX = Math.Abs(obj1CenterX - obj2CenterX);
        int distanceY = Math.Abs(obj1CenterY - obj2CenterY);

        return distanceX < obj1W / 2 + obj2W / 2 &&
               distanceY < obj1H / 2 + obj2H / 2;
    }
}
