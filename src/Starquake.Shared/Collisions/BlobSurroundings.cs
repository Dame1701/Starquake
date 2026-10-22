// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Core;

namespace Starquake.Collisions;

/// <summary>
///     What Blob can reach in each of the four directions this frame: the tile he would meet if he
///     carried on that way, or <c>null</c> for open space. Nearly everything Blob does about the
///     world he is standing in is decided from these four readings - the shuttle pad, the teleporter,
///     the access booths, both tunnel mouths, a bridge underfoot and the lift shaft overhead.
/// </summary>
/// <remarks>
///     Passed as one value rather than kept as fields, so nothing silently depends on a probe having
///     run first. <c>Blob.UpdateNormalState</c> deliberately acts on the previous frame's reading;
///     the frame boundary is written down there.
/// </remarks>
internal readonly record struct BlobSurroundings(
    TileDetected? Above,
    TileDetected? Below,
    TileDetected? Left,
    TileDetected? Right)
{
    /// <summary>Probes the four directions around something of this size at this position.</summary>
    /// <remarks>
    ///     <paramref name="heightBelow" /> is not always <paramref name="height" />. Blob on the
    ///     shuttle is taller, with the shuttle underneath, so the ground is looked for from Blob's own
    ///     feet. The caller does the subtraction, because the shuttle's height is its business.
    /// </remarks>
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
