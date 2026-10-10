// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.Core;
using Starquake.SharedLibs;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The indexed collision map measured against <see cref="ReferenceCollisionMap" />, which is
///     the flat list it replaced, over the planet the game actually ships.
/// </summary>
/// <remarks>
///     The indexed map is faster, and has to answer the same. These compare the two everywhere
///     rather than at a handful of chosen spots, and report a disagreement with the screen, the
///     position and the direction that found it rather than as a bare count.
///     <para>
///         The shipped screens carry no pick-ups or power-ups - those are placed by
///         <c>WorldPopulator</c> at the start of a game, not stored in the map - so the items and
///         the dynamic sources are covered by <see cref="CollisionMapParityTests" /> on screens
///         built by hand instead.
///     </para>
/// </remarks>
public class CollisionMapDifferentialTests
{
    /// <summary>
    ///     How far apart the sampled positions are. Deliberately not a multiple of the 32-pixel
    ///     cell, so the sweep lands inside cells as well as on their edges, and offset so it does
    ///     not start on a tile boundary either.
    /// </summary>
    private const int PositionStride = 37;

    /// <inheritdoc cref="PositionStride" />
    private const int PositionOffset = -11;

    private static readonly int[] Velocities = [1, 4, 8, 12];

    private static readonly Direction[] Directions =
        [Direction.Up, Direction.Down, Direction.Left, Direction.Right];

    [Fact]
    public void Rebuild_OverEveryShippedScreen_ProducesTheSameCells()
    {
        // Arrange
        (GameScreenData[] screens, int[,,] masks) = ShippedMap.Load();
        var subject = new ScreenCollisionMap(screens, masks);
        var reference = new ReferenceCollisionMap(screens, masks);
        List<string> disagreements = [];

        // Act
        for (int screen = 0; screen < screens.Length; screen++)
        {
            subject.Rebuild(screen);
            reference.Rebuild(screen);

            HashSet<(int, int, int, CollisionTileType)> fromSubject =
                [.. subject.Cells.Select(c => (c.TileImage, c.XPos, c.YPos, c.CollisionTileType))];
            HashSet<(int, int, int, CollisionTileType)> fromReference =
                [.. reference.Cells.Select(c => (c.TileImage, c.XPos, c.YPos, c.CollisionTileType))];

            if (!fromSubject.SetEquals(fromReference))
            {
                disagreements.Add(
                    $"screen {screen}: only in map [{Describe(fromSubject.Except(fromReference))}], " +
                    $"only in reference [{Describe(fromReference.Except(fromSubject))}]");
            }
        }

        // Assert
        Assert.Empty(disagreements);
    }

    [Fact]
    public void CheckCollision_OverEveryShippedScreen_AllowsTheSameDistance()
    {
        // Arrange
        (GameScreenData[] screens, int[,,] masks) = ShippedMap.Load();
        var subject = new ScreenCollisionMap(screens, masks);
        var reference = new ReferenceCollisionMap(screens, masks);
        List<string> disagreements = [];

        // Act
        for (int screen = 0; screen < screens.Length; screen++)
        {
            subject.Rebuild(screen);
            reference.Rebuild(screen);

            foreach ((int x, int y) in SweptPositions())
            {
                foreach (Direction direction in Directions)
                {
                    foreach (bool isBlob in (bool[])[true, false])
                    {
                        int velocity = Velocities[(x + y + (int)direction) & 3];

                        int fromSubject = subject.CheckCollision(
                            direction, x, y, SweepWidth, SweepHeight, velocity, isBlob);
                        int fromReference = reference.CheckCollision(
                            direction, x, y, SweepWidth, SweepHeight, velocity, isBlob);

                        if (fromSubject != fromReference && disagreements.Count < 20)
                        {
                            disagreements.Add(
                                $"screen {screen} at ({x},{y}) {direction} v{velocity} " +
                                $"isBlob={isBlob}: map {fromSubject}, reference {fromReference}");
                        }
                    }
                }
            }
        }

        // Assert
        Assert.Empty(disagreements);
    }

    [Fact]
    public void CheckRectangles_OverEveryShippedScreen_ReportsTheSameHit()
    {
        // Arrange
        (GameScreenData[] screens, int[,,] masks) = ShippedMap.Load();
        var subject = new ScreenCollisionMap(screens, masks);
        var reference = new ReferenceCollisionMap(screens, masks);
        List<string> disagreements = [];

        // Act
        for (int screen = 0; screen < screens.Length; screen++)
        {
            subject.Rebuild(screen);
            reference.Rebuild(screen);

            foreach ((int x, int y) in SweptPositions())
            {
                foreach (bool isBlob in (bool[])[true, false])
                {
                    bool subjectHit = subject.CheckRectangles(x, y, SweepWidth, SweepHeight, isBlob) >= 0;
                    bool referenceHit = reference.CheckRectangles(x, y, SweepWidth, SweepHeight, isBlob) >= 0;

                    if (subjectHit != referenceHit && disagreements.Count < 20)
                    {
                        disagreements.Add(
                            $"screen {screen} at ({x},{y}) isBlob={isBlob}: " +
                            $"map {subjectHit}, reference {referenceHit}");
                    }
                }
            }
        }

        // Assert
        Assert.Empty(disagreements);
    }

    /// <summary>
    ///     The question <c>NastiesMayStandOn</c> asks, over every cell of every shipped screen. This
    ///     is the one that used to scan the whole list once per cell, and the one a grid that stored
    ///     a single kind per cell would quietly get wrong where a tile is solid and nasty-only at
    ///     once.
    /// </summary>
    [Fact]
    public void HasNastyOnlyCellAt_OverEveryCellOfEveryShippedScreen_Agrees()
    {
        // Arrange
        (GameScreenData[] screens, int[,,] masks) = ShippedMap.Load();
        var subject = new ScreenCollisionMap(screens, masks);
        var reference = new ReferenceCollisionMap(screens, masks);
        List<string> disagreements = [];

        // Act
        for (int screen = 0; screen < screens.Length; screen++)
        {
            subject.Rebuild(screen);
            reference.Rebuild(screen);

            for (int cellY = 0; cellY < TileMetrics.PlayAreaHeight; cellY += TileMetrics.CollisionCellHeight)
            {
                for (int cellX = 0; cellX < TileMetrics.PlayAreaWidth; cellX += TileMetrics.CollisionCellWidth)
                {
                    bool fromSubject = subject.HasNastyOnlyCellAt(cellX, cellY);
                    bool fromReference = reference.HasNastyOnlyCellAt(cellX, cellY);

                    if (fromSubject != fromReference && disagreements.Count < 20)
                    {
                        disagreements.Add(
                            $"screen {screen} cell ({cellX},{cellY}): " +
                            $"map {fromSubject}, reference {fromReference}");
                    }
                }
            }
        }

        // Assert
        Assert.Empty(disagreements);
    }

    private const int SweepWidth = 64;

    private const int SweepHeight = 64;

    /// <summary>
    ///     The positions a sweep visits: across the screen and a little beyond each edge, because
    ///     Blob is not held inside one and a nasty being clamped is part of what is being compared.
    /// </summary>
    private static IEnumerable<(int X, int Y)> SweptPositions()
    {
        for (int y = PositionOffset; y < TileMetrics.PlayAreaHeight; y += PositionStride)
        {
            for (int x = PositionOffset; x < TileMetrics.PlayAreaWidth; x += PositionStride)
            {
                yield return (x, y);
            }
        }
    }

    private static string Describe(IEnumerable<(int Image, int X, int Y, CollisionTileType Type)> cells)
        => string.Join("; ", cells.Take(5).Select(c => $"{c.Image}@({c.X},{c.Y}) {c.Type}"));
}
