// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Collisions;

namespace Starquake.Structure;

/// <summary>
///     What the blob can reach in each of the four directions this frame: the tile it would meet if it
///     carried on that way, or <c>null</c> for open space. Nearly everything the blob does about the
///     world it is standing in is decided from these four readings - the shuttle pad, the teleporter,
///     the access booths, both tunnel mouths, a bridge underfoot and the lift shaft overhead.
/// </summary>
/// <remarks>
///     This was four fields on <see cref="T:Starquake.ActiveSprites.Blob" />, written by one method and
///     read by eight others that said nothing about depending on it. Eight methods silently required
///     that method to have run first, and one of them deliberately runs before it - see
///     <c>Blob.UpdateNormalState</c>, where the frame boundary is now written down.
/// </remarks>
internal readonly record struct BlobSurroundings(
    TileDetected? Above,
    TileDetected? Below,
    TileDetected? Left,
    TileDetected? Right)
{
    /// <summary>
    ///     Probes the four directions around something of this size at this position.
    /// </summary>
    /// <param name="heightBelow">
    ///     The height to probe downwards with, which is not always <paramref name="height" />: the blob
    ///     riding the shuttle is taller than the blob, and the shuttle is underneath him, so the ground
    ///     has to be looked for from his own feet rather than the shuttle's. The caller subtracts,
    ///     because the shuttle's height is the caller's business.
    /// </param>
    public static BlobSurroundings Around(
        ScreenCollisionMap collisions,
        int xPos,
        int yPos,
        int width,
        int height,
        int heightBelow,
        int horizontalSpeed,
        int verticalSpeed)
        => new(
            collisions.CheckTileByObject(Direction.Up, xPos, yPos, width, height, verticalSpeed),
            collisions.CheckTileByObject(Direction.Down, xPos, yPos, width, heightBelow, verticalSpeed),
            collisions.CheckTileByObject(Direction.Left, xPos, yPos, width, height, horizontalSpeed),
            collisions.CheckTileByObject(Direction.Right, xPos, yPos, width, height, horizontalSpeed));
}
