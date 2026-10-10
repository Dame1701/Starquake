// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Collisions;
using Starquake.Core;

namespace Starquake.ActiveSprites;

/// <summary>
///     Works out where Blob has to move to get free when buried in scenery. That happens when a tile
///     appears underneath: a shuttle platform being taken, or a screen changing beneath Blob's feet.
/// </summary>
/// <remarks>
///     A question rather than a command. Given where Blob is, this answers where Blob should be, and
///     Blob does the moving. The only thing needed besides Blob's own box is the collision map.
/// </remarks>
internal class UnstickProbe
{
    /// <summary>
    ///     How thick a slice is tested when working out which way a stuck Blob must be nudged.
    /// </summary>
    private const int UnstickProbeThickness = 8;

    private readonly ScreenCollisionMap _collisions;

    public UnstickProbe(ScreenCollisionMap collisions)
    {
        _collisions = collisions;
    }

    /// <summary>
    ///     Where Blob should be, given where he is. Returns the same position when not stuck.
    /// </summary>
    /// <remarks>
    ///     Each edge is tried in turn, and only when Blob is caught on that edge and free at the
    ///     opposite one. Caught on both means there is nowhere to push to, so Blob stays put.
    /// </remarks>
    public Point FreePositionFor(Rectangle blob)
    {
        blob = FreeOfEdge(blob, Direction.Up);
        blob = FreeOfEdge(blob, Direction.Down);
        blob = FreeOfEdge(blob, Direction.Left);
        blob = FreeOfEdge(blob, Direction.Right);

        return blob.Location;
    }

    /// <summary>Which way Blob must move to get off an edge he is caught on.</summary>
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

    /// <summary>The thin strip along one edge of Blob tested for being buried in scenery.</summary>
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

        // Walk the probe away from the edge until it comes free, and move Blob that far.
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
