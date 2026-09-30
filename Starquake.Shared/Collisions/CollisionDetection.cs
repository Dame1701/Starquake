// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Structure;

namespace Starquake.Collisions;

public static class CollisionDetection
{
    private static int _obj1CenterX;
    private static int _obj1CenterY;

    private static int _obj2CenterX;
    private static int _obj2CenterY;

    private static int _distanceX;
    private static int _distanceY;

    private static bool _coordColY;
    private static bool _coordColX;

    /// <summary>
    ///     Checks for a collision between two objects and returns the amount of distance that it is legal to travel
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="boundingBox1Velocity"></param>
    /// <param name="boundingBox1"></param>
    /// <param name="boundingBox2"></param>
    /// <returns></returns>
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
    /// <param name="direction"></param>
    /// <param name="Object1Velocity"></param>
    /// <param name="obj1X"></param>
    /// <param name="obj1Y"></param>
    /// <param name="obj1W"></param>
    /// <param name="obj1H"></param>
    /// <param name="obj2X"></param>
    /// <param name="obj2Y"></param>
    /// <param name="obj2W"></param>
    /// <param name="obj2H"></param>
    /// <returns></returns>
    public static int CheckDirection(Direction direction, int obj1Velocity,
        int obj1X, int obj1Y, int obj1W, int obj1H,
        int obj2X, int obj2Y, int obj2W, int obj2H)
    {
        (int stepX, int stepY) = StepFor(direction);

        if (stepX == 0 && stepY == 0)
        {
            return 0;
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
    ///     Checks for a collision and if it has taken place return the direction it occurred in
    /// </summary>
    /// <param name="Direction"></param>
    /// <param name="Object1Velocity"></param>
    /// <param name="obj1X"></param>
    /// <param name="obj1Y"></param>
    /// <param name="obj1W"></param>
    /// <param name="obj1H"></param>
    /// <param name="obj2X"></param>
    /// <param name="obj2Y"></param>
    /// <param name="obj2W"></param>
    /// <param name="obj2H"></param>
    /// <returns></returns>
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

    public static bool Check(int obj1X, int obj1Y, int obj1W, int obj1H,
        int obj2X, int obj2Y, int obj2W, int obj2H)
    {
        _coordColY = false;
        _coordColX = false;

        // Calculate centre point of each object
        // object 1
        _obj1CenterX = obj1X + obj1W / 2;
        _obj1CenterY = obj1Y + obj1H / 2;

        // object 2
        _obj2CenterX = obj2X + obj2W / 2;
        _obj2CenterY = obj2Y + obj2H / 2;

        // Calculate the distances between the two center points
        _distanceX = _obj1CenterX - _obj2CenterX;
        _distanceY = _obj1CenterY - _obj2CenterY;

        // Identify where the objects are in relation to each other
        // Check y coordinates
        if (_obj1CenterY - _obj2CenterY > 0)
        {
            _distanceY = _obj1CenterY - _obj2CenterY;
        }
        else
        {
            _distanceY = _obj2CenterY - _obj1CenterY;
        }

        // Calculate distance between the two center points
        if (_obj1CenterX - _obj2CenterX > 0)
        {
            _distanceX = _obj1CenterX - _obj2CenterX;
        }
        else
        {
            _distanceX = _obj2CenterX - _obj1CenterX;
        }

        // Check for a collission
        if (_distanceX < obj1W / 2 + obj2W / 2)
        {
            _coordColX = true;
        }

        if (_distanceY < obj1H / 2 + obj2H / 2)
        {
            _coordColY = true;
        }

        // Return the results
        if (_coordColX && _coordColY)
        {
            return true;
        }
        return false;
    }
}
