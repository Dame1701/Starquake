// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.Core;

namespace Starquake.ActiveSprites.BlobParts;

/// <summary>
///     How Blob falls: how fast, how far he has come, and how far he is allowed to drop this frame.
///     Not real physics - he falls at a fixed speed that steps up the longer the drop goes on, exactly as
///     the original did.
/// </summary>
/// <remarks>
///     Four counters and the arithmetic over them, which is the part of falling that decides anything.
///     What is left behind in <see cref="Blob" /> is the two things that are not arithmetic: moving him
///     down by what this returns, and the sound of a long fall.
/// </remarks>
internal sealed class BlobFall
{
    /// <summary>
    ///     How fast he starts falling, how fast he can ever fall, and how far he has to drop to gain a
    ///     pixel a frame. All three in pixels, and the speed in pixels a logic frame.
    /// </summary>
    private const int InitialSpeed = 4;

    /// <inheritdoc cref="InitialSpeed" />
    private const int MaxSpeed = 12;

    /// <inheritdoc cref="InitialSpeed" />
    private const int SpeedIncreaseDistance = 12;

    /// <summary>
    ///     How far he has to have dropped before the fall is worth hearing. A sprite's height, so a drop
    ///     from one platform to the next below it is silent.
    /// </summary>
    private const int AudibleDistance = SpriteMetrics.SpriteHeight;

    /// <summary>How far he has dropped since the last step up in speed, and since the fall began.</summary>
    private int _distanceFallen;

    private int _speed = InitialSpeed;

    private int _totalDistanceFallen;

    /// <summary>
    ///     How far he has fallen without landing. Read by the walk cycle, which will not play while he is
    ///     in the air, and by the breakable bridges, which only give way under a hard enough landing.
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    ///     Back to the speed a fall starts at, for when he has just arrived somewhere - over the bottom
    ///     edge of a screen, or back at his entry point after dying. The total is deliberately not
    ///     cleared: a drop through two screens is one fall, and he lands at the bottom of it just as hard.
    /// </summary>
    public void StartFallingAgain()
    {
        _distanceFallen = 0;
        _speed = InitialSpeed;
    }

    /// <summary>
    ///     One logic frame of falling. Returns how far down he goes and whether the fall is worth hearing;
    ///     moving him is the caller's job, because only the caller knows what else has moved him this
    ///     frame.
    /// </summary>
    /// <param name="collisions">The screen he is falling through.</param>
    /// <param name="blob">Where he is and how big he is.</param>
    /// <param name="movedLeft">Whether he is being steered left, for the tunnel-mouth shuffle.</param>
    /// <param name="movedRight">Whether he is being steered right, for the same.</param>
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
    ///     Blob falling past the mouth of a tunnel or a booth, with the player steering him into it, is
    ///     shuffled sideways rather than dropping past it. Without this the opening is almost impossible
    ///     to hit, because a fall covers more than its height in a frame.
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
