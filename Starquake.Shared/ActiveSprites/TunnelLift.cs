// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Collisions;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

/// <summary>
///     The lift that carries Blob up a shaft. He gets on by walking under one, goes up until the
///     shaft runs out, and can step off into a side passage once he has gone far enough up to have
///     cleared the one he got on at.
/// </summary>
/// <remarks>
///     Going up a shaft is a mode: while he is in one he does not fall, cannot reach his inventory, and
///     does not leave over the top edge of the screen. Those three guards stay with <see cref="Blob" />
///     and read <see cref="GoingUp" />; what moved here is the decision about when he is on one and how
///     far it carries him.
///     <para>
///         The lift's sound is two callbacks rather than a sound engine, which is what keeps this class
///         reachable from a test - the same trade <see cref="BlobFall" /> makes by handing its caller a
///         flag. The looping sound is asked for on every frame of the way up; the sound engine keeps one
///         instance of it.
///     </para>
/// </remarks>
internal sealed class TunnelLift
{
    /// <summary>
    ///     How fast the lift rises, in pixels a logic frame, and how far he has to have gone up before he
    ///     is allowed to step off sideways. Stepping off any sooner would put him straight back into the
    ///     passage he arrived through.
    /// </summary>
    private const int Speed = 8;

    /// <inheritdoc cref="Speed" />
    private const int MinimumDistanceUp = 8;

    /// <summary>
    ///     Where in the tile over his head the shaft is recognised: across its middle for getting on, and
    ///     a tile's height down for the top of it and for a side passage.
    /// </summary>
    private const int ShaftCentre = 32;

    /// <inheritdoc cref="ShaftCentre" />
    private const int ShaftTop = 96;

    private readonly Action _startSound;
    private readonly Action _stopSound;

    private int _distanceGoneUp;

    public TunnelLift(Action startSound, Action stopSound)
    {
        _startSound = startSound;
        _stopSound = stopSound;
    }

    /// <summary>
    ///     Whether he is going up a shaft. Read by the three things it suspends - falling, the inventory,
    ///     and leaving over the top of the screen.
    /// </summary>
    public bool GoingUp { get; private set; }

    /// <summary>
    ///     Puts him off the lift without stepping it, for when he has just died on one.
    /// </summary>
    public void LetGo() => GoingUp = false;

    /// <summary>
    ///     One logic frame of the lift. Returns how far it carries him up, which is nought when he is not
    ///     on one and nought when he is stepping off.
    /// </summary>
    /// <param name="collisions">The screen he is going up through.</param>
    /// <param name="blob">Where he is and how big he is.</param>
    /// <param name="above">The tile over his head, which is what the shaft is recognised by.</param>
    /// <param name="movedLeft">Whether he is being steered left, out of the shaft.</param>
    /// <param name="movedRight">Whether he is being steered right, out of the shaft.</param>
    /// <param name="horizontalSpeed">How far he would walk in a frame, for testing the side passage.</param>
    public int Step(
        ScreenCollisionMap collisions,
        Rectangle blob,
        TileDetected? above,
        bool movedLeft,
        bool movedRight,
        int horizontalSpeed)
    {
        GetOnOrOff(above);

        if (!GoingUp)
        {
            return 0;
        }

        if (SteppingOffIntoASidePassage(collisions, blob, above, movedLeft, movedRight, horizontalSpeed))
        {
            GoingUp = false;
            _stopSound();

            return 0;
        }

        _startSound();
        _distanceGoneUp += Speed;

        return Speed;
    }

    private void GetOnOrOff(TileDetected? above)
    {
        if (above == null)
        {
            return;
        }

        switch (above.TileNumber)
        {
            case (int)ActionTiles.TunnelLiftShaft1 or
                (int)ActionTiles.TunnelLiftShaft2 or
                (int)ActionTiles.TunnelLiftTile or
                (int)ActionTiles.TunnelLiftShaft3:
            {
                // Only under the middle of the shaft, so walking past the edge of one does not grab him.
                if (above.XPosition == ShaftCentre)
                {
                    if (!GoingUp)
                    {
                        _distanceGoneUp = 0;
                    }

                    GoingUp = true;
                }

                break;
            }
            case (int)ActionTiles.TunnelLiftTop1 or
                (int)ActionTiles.TunnelLiftTop2 or
                (int)ActionTiles.TunnelLiftTop3:
            {
                if (above.YPosition == ShaftTop)
                {
                    GoingUp = false;
                }

                break;
            }
        }
    }

    /// <summary>
    ///     Whether he is being steered out of the shaft into a passage he can actually walk into, and has
    ///     gone up far enough to be allowed to.
    /// </summary>
    private bool SteppingOffIntoASidePassage(
        ScreenCollisionMap collisions,
        Rectangle blob,
        TileDetected? above,
        bool movedLeft,
        bool movedRight,
        int horizontalSpeed)
    {
        if (above?.TileNumber is not ((int)ActionTiles.TunnelLiftShaft1 or
            (int)ActionTiles.TunnelLiftShaft2 or
            (int)ActionTiles.TunnelLiftShaft3))
        {
            return false;
        }

        if (above.YPosition != ShaftTop)
        {
            return false;
        }

        if (_distanceGoneUp <= MinimumDistanceUp)
        {
            return false;
        }

        // Either way out will do. The original tested left and then right in an else-if, so holding both
        // with a wall to the left still steps him off to the right; keeping that rather than returning on
        // the first direction asked about.
        return movedLeft && RoomToWalk(collisions, blob, Direction.Left, horizontalSpeed)
               || movedRight && RoomToWalk(collisions, blob, Direction.Right, horizontalSpeed);
    }

    private static bool RoomToWalk(
        ScreenCollisionMap collisions,
        Rectangle blob,
        Direction direction,
        int horizontalSpeed)
        => collisions.CheckCollision(
            direction, blob.X, blob.Y, blob.Width, blob.Height, horizontalSpeed, true) > 0;
}
