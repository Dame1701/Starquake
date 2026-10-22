// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.Collisions;
using Starquake.Core;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

/// <summary>
///     The lift that carries Blob up a shaft. Blob gets on by walking under one, rises until the
///     shaft runs out, and can step off into a side passage once high enough to have cleared the
///     passage boarded from.
/// </summary>
/// <remarks>
///     Being on a lift is a mode: Blob does not fall, cannot reach the inventory, and does not leave
///     over the top edge of the screen. Those three guards live in <see cref="Blob" /> and read
///     <see cref="GoingUp" />. What is here is when Blob is on a lift and how far it carries him.
///     <para>
///         The lift's sound is two callbacks rather than a sound engine, which keeps this class
///         reachable from a test. The looping sound is asked for on every frame of the way up, and the
///         sound engine keeps one instance of it.
///     </para>
/// </remarks>
internal sealed class TunnelLift
{
    /// <summary>
    ///     How fast the lift rises, in pixels a logic frame, and how far up Blob must get before
    ///     stepping off sideways is allowed. Any sooner would put Blob straight back into the passage
    ///     arrived through.
    /// </summary>
    private const int Speed = 8;

    /// <inheritdoc cref="Speed" />
    private const int MinimumDistanceUp = 8;

    /// <summary>
    ///     Where in the tile overhead the shaft is recognised: across its middle for getting on, and a
    ///     tile's height down for the top of the shaft and for a side passage.
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
    ///     Whether Blob is going up a shaft. Read by the three things it suspends: falling, the
    ///     inventory, and leaving over the top of the screen.
    /// </summary>
    public bool GoingUp { get; private set; }

    /// <summary>Puts Blob off the lift without stepping it, after dying on one.</summary>
    public void LetGo() => GoingUp = false;

    /// <summary>One logic frame of the lift.</summary>
    /// <param name="collisions">The screen being gone up through.</param>
    /// <param name="blob">Blob's position and size.</param>
    /// <param name="above">The tile overhead, which is how a shaft is recognised.</param>
    /// <param name="movedLeft">Whether Blob is being steered left, out of the shaft.</param>
    /// <param name="movedRight">Whether Blob is being steered right, out of the shaft.</param>
    /// <param name="horizontalSpeed">How far Blob walks in a frame, for testing the side passage.</param>
    /// <returns>How far the lift carries Blob up. Nought when not on one, and when stepping off.</returns>
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
                // Only under the middle of the shaft, so walking past the edge does not grab Blob.
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
    ///     Whether Blob is being steered into a side passage that can actually be walked into, and
    ///     has risen far enough to be allowed to.
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

        // Either way out will do, so both directions are tested rather than returning on the first.
        // Holding both with a wall to the left still steps Blob off to the right.
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
