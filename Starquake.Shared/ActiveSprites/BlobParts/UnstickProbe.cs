// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Collisions;
using Starquake.Structure;

namespace Starquake.ActiveSprites.BlobParts;

/// <summary>
///     Works out where Blob has to be moved to get free when he is buried in scenery. That happens
///     when a tile appears underneath him - a shuttle platform being taken, a screen changing
///     beneath his feet.
/// </summary>
/// <remarks>
///     The plan called this the place to start on <c>Blob</c> because it was already the one part of
///     that class testable today: four of its five methods were static or took only a rectangle, and
///     the only thing it wants besides Blob's own box is the collision map.
///     <para>
///         It is a question rather than a command - given where Blob is, where should he be -
///         so Blob does the moving and this does the arithmetic.
///     </para>
/// </remarks>
internal class UnstickProbe
{
    /// <summary>
    ///     How thick a slice is tested when working out which way a stuck Blob has to be nudged to
    ///     get free after a tile map change.
    /// </summary>
    private const int UnstickProbeThickness = 8;

    private readonly ScreenCollisionMap _collisions;

    public UnstickProbe(ScreenCollisionMap collisions) => _collisions = collisions;

    /// <summary>
    ///     Where Blob should be, given where he is. The same position back if he is not stuck.
    /// </summary>
    /// <remarks>
    ///     Each edge is tried in turn, and only if he is caught on that edge and free at the opposite
    ///     one: caught on both means there is nowhere to push him and he is left where he is. The four
    ///     edges were four copies of this, differing only in which thin strip is probed and which way
    ///     he is then walked.
    /// </remarks>
    public Point FreePositionFor(Rectangle blob)
    {
        blob = FreeOfEdge(blob, Direction.Up);
        blob = FreeOfEdge(blob, Direction.Down);
        blob = FreeOfEdge(blob, Direction.Left);
        blob = FreeOfEdge(blob, Direction.Right);

        return blob.Location;
    }

    /// <summary>
    ///     Which way Blob has to move to get off an edge he is caught on.
    /// </summary>
    private static Point PushAwayFrom(Direction edge) => edge switch
    {
        Direction.Up => new Point(0, 1),
        Direction.Down => new Point(0, -1),
        Direction.Left => new Point(1, 0),
        _ => new Point(-1, 0)
    };

    private static Direction OppositeOf(Direction edge) => edge switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        _ => Direction.Left
    };

    /// <summary>
    ///     The thin strip along one edge of Blob that is tested for being buried in scenery.
    /// </summary>
    private static Rectangle EdgeProbe(Rectangle blob, Direction edge) => edge switch
    {
        Direction.Up => new Rectangle(blob.X, blob.Y, blob.Width, UnstickProbeThickness),
        Direction.Down => new Rectangle(
            blob.X,
            blob.Y + (blob.Height - UnstickProbeThickness),
            blob.Width,
            UnstickProbeThickness),
        Direction.Left => new Rectangle(blob.X, blob.Y, UnstickProbeThickness, blob.Height),
        _ => new Rectangle(
            blob.X + (blob.Width - UnstickProbeThickness),
            blob.Y,
            UnstickProbeThickness,
            blob.Height)
    };

    private Rectangle FreeOfEdge(Rectangle blob, Direction edge)
    {
        Rectangle caughtOn = EdgeProbe(blob, edge);
        Rectangle oppositeEdge = EdgeProbe(blob, OppositeOf(edge));
        Point push = PushAwayFrom(edge);

        if (!IsInsideScenery(caughtOn) || IsInsideScenery(oppositeEdge))
        {
            return blob;
        }

        // Walk the probe away from the edge until it comes free, and move him that far
        int distanceToMove = 0;

        while (IsInsideScenery(new Rectangle(
                   caughtOn.X + push.X * distanceToMove,
                   caughtOn.Y + push.Y * distanceToMove,
                   caughtOn.Width,
                   caughtOn.Height)))
        {
            distanceToMove++;
        }

        blob.Offset(push.X * distanceToMove, push.Y * distanceToMove);

        return blob;
    }

    private bool IsInsideScenery(Rectangle probe)
        => _collisions.CheckRectangles(probe.X, probe.Y, probe.Width, probe.Height, true) >= 0;
}
