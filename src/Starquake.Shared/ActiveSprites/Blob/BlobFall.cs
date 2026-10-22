// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.Core;

namespace Starquake.ActiveSprites;

/// <summary>
///     How Blob falls: how fast, how far so far, and how far to drop this frame. Not real physics.
///     The speed is fixed and steps up the longer the drop lasts, exactly as the original did.
/// </summary>
/// <remarks>
///     Four counters and the arithmetic over them. <see cref="Blob" /> keeps the two parts that are
///     not arithmetic: moving down by what this returns, and the sound of a long fall.
/// </remarks>
internal sealed class BlobFall
{
    /// <summary>
    ///     The starting speed, the top speed, and how far a fall must go to gain a pixel a frame.
    ///     All in pixels; the speeds in pixels a logic frame.
    /// </summary>
    private const int InitialSpeed = 4;

    /// <inheritdoc cref="InitialSpeed" />
    private const int MaxSpeed = 12;

    /// <inheritdoc cref="InitialSpeed" />
    private const int SpeedIncreaseDistance = 12;

    /// <summary>
    ///     How far a fall must go before it is worth hearing. One sprite height, so dropping from one
    ///     platform to the next below is silent.
    /// </summary>
    private const int AudibleDistance = SpriteMetrics.SpriteHeight;

    /// <summary>How far has been dropped since the last step up in speed, and since the fall began.</summary>
    private int _distanceFallen;

    private int _speed = InitialSpeed;

    private int _totalDistanceFallen;

    /// <summary>
    ///     How far Blob has fallen without landing. Read by the walk cycle, which does not play in
    ///     mid-air, and by the breakable bridges, which only give way under a hard enough landing.
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    ///     Resets to the speed a fall starts at, after arriving over a screen's bottom edge or back at
    ///     the entry point. The total is deliberately not cleared: a drop through two screens is one
    ///     fall, and the landing at the bottom is just as hard.
    /// </summary>
    public void StartFallingAgain()
    {
        _distanceFallen = 0;
        _speed = InitialSpeed;
    }

    /// <summary>One logic frame of falling.</summary>
    /// <param name="collisions">The screen being fallen through.</param>
    /// <param name="blob">Blob's position and size.</param>
    /// <param name="movedLeft">Whether Blob is being steered left, for the tunnel-mouth shuffle.</param>
    /// <param name="movedRight">Whether Blob is being steered right, for the same.</param>
    /// <returns>
    ///     How far down to move and whether the fall is worth hearing. Doing the moving is the
    ///     caller's job, because only the caller knows what else has moved Blob this frame.
    /// </returns>
    public Fall Step(ScreenCollisionMap collisions, Rectangle blob, bool movedLeft, bool movedRight)
    {
        int falling = DistanceToTheGround(collisions, blob);

        if (falling > 0)
        {
            _distanceFallen += falling;
            _totalDistanceFallen += falling;

            if (_distanceFallen > SpeedIncreaseDistance && _speed < MaxSpeed)
            {
                _speed++;
                _distanceFallen = 0;
            }
        }
        else
        {
            _totalDistanceFallen = 0;
            _distanceFallen = 0;
            _speed = InitialSpeed;
            Height = 0;
        }

        // Asked again because the step up in speed above may have changed the answer.
        int fallDistance = DistanceToTheGround(collisions, blob);

        bool audible = fallDistance > 0 && _totalDistanceFallen > AudibleDistance;

        fallDistance = EdgeShuffleTowardOpening(collisions, blob, fallDistance, movedLeft, movedRight);

        Height += fallDistance;

        return new Fall(fallDistance, audible, falling == 0);
    }

    private int DistanceToTheGround(ScreenCollisionMap collisions, Rectangle blob)
        => collisions.CheckCollision(Direction.Down, blob.X, blob.Y, blob.Width, blob.Height, _speed, true);

    /// <summary>
    ///     Shuffles Blob sideways into a tunnel or booth mouth being fallen past while the player
    ///     steers towards it. Without this the opening is nearly impossible to hit, because a fall
    ///     covers more than the opening's height in one frame.
    /// </summary>
    private static int EdgeShuffleTowardOpening(
        ScreenCollisionMap collisions,
        Rectangle blob,
        int fallDistance,
        bool movedLeft,
        bool movedRight)
    {
        if (movedRight)
        {
            for (int a = 0; a < fallDistance; a++)
            {
                if (collisions.CheckCollision(Direction.Down, blob.X + blob.Width,
                        blob.Y + a, blob.Width + 1, blob.Height, 1, true) == 0)
                {
                    if (collisions.CheckCollision(Direction.Right, blob.X + blob.Width,
                            blob.Y + a, blob.Width, blob.Height, 1, true) > 0)
                    {
                        fallDistance = a;

                        if (fallDistance == 0)
                        {
                            fallDistance = 1;
                        }

                        break;
                    }
                }
            }
        }

        if (movedLeft)
        {
            for (int a = 0; a < fallDistance; a++)
            {
                if (collisions.CheckCollision(Direction.Down, blob.X - blob.Width,
                        blob.Y + a, blob.Width + 1, blob.Height, 1, true) == 0)
                {
                    if (collisions.CheckCollision(Direction.Right, blob.X - blob.Width,
                            blob.Y + a, blob.Width, blob.Height, 1, true) > 0)
                    {
                        fallDistance = a;

                        if (fallDistance == 0)
                        {
                            fallDistance = 1;
                        }

                        break;
                    }
                }
            }
        }

        return fallDistance;
    }
}
