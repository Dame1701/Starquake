// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Collisions;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     <c>CheckDirection</c> walks a move one pixel at a time, and skips the walk for a box the
///     mover cannot reach however far it steps. The skip is meant to be an optimisation and
///     nothing else, so what matters is that it never changes an answer.
/// </summary>
/// <remarks>
///     These compare the real thing against the walk written out plainly, over every case rather
///     than over a chosen few. A rejection that is a pixel too eager would let a mover through a
///     wall, which no other test here would notice.
/// </remarks>
public class CollisionBroadPhaseTests
{
    // Internal test methods rather than public, as in CollisionDetectionTests and for the same
    // reason: Direction is one of the game's own types and those are internal, which a public
    // signature here cannot name.

    /// <summary>
    ///     The walk as it was before anything was skipped: step by step, stop when the boxes touch.
    /// </summary>
    private static int WalkedOneStepAtATime(Direction direction, int velocity,
        int obj1X, int obj1Y, int obj1W, int obj1H,
        int obj2X, int obj2Y, int obj2W, int obj2H)
    {
        (int stepX, int stepY) = direction switch
        {
            Direction.Up => (0, -1),
            Direction.Down => (0, 1),
            Direction.Left => (-1, 0),
            Direction.Right => (1, 0),
            _ => (0, 0)
        };

        if (stepX == 0 && stepY == 0)
        {
            return 0;
        }

        int moved = 0;

        for (int step = 1; step <= velocity; step++)
        {
            if (CollisionDetection.Check(obj1X + stepX * step, obj1Y + stepY * step, obj1W, obj1H,
                    obj2X, obj2Y, obj2W, obj2H))
            {
                break;
            }

            moved++;
        }

        return moved;
    }

    /// <summary>
    ///     Every cell position on a screen, against a mover the size of Blob, in all four
    ///     directions - which is the case the skip was written for and the one it runs in.
    /// </summary>
    [Theory]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Right)]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Down)]
    internal void CheckDirection_AgainstEveryCellOnAScreen_AgreesWithTheWalk(Direction direction)
    {
        // Arrange
        int disagreements = 0;

        // Act
        for (int cellY = 0; cellY < TestData.PlayAreaHeight; cellY += TestData.CellSize)
        {
            for (int cellX = 0; cellX < TestData.PlayAreaWidth; cellX += TestData.CellSize)
            {
                int walked = WalkedOneStepAtATime(direction, TestData.Velocity,
                    TestData.MoverX, TestData.MoverY, TestData.MoverWidth, TestData.MoverHeight,
                    cellX, cellY, TestData.CellSize, TestData.CellSize);

                int checked_ = CollisionDetection.CheckDirection(direction, TestData.Velocity,
                    TestData.MoverX, TestData.MoverY, TestData.MoverWidth, TestData.MoverHeight,
                    cellX, cellY, TestData.CellSize, TestData.CellSize);

                if (walked != checked_)
                {
                    disagreements++;
                }
            }
        }

        // Assert
        Assert.Equal(0, disagreements);
    }

    /// <summary>
    ///     The boundary the skip is most likely to get wrong: a cell exactly as far away as the
    ///     mover can reach, one pixel nearer, and one pixel further. Swept across every offset a
    ///     cell can sit at relative to the mover, on both axes, at several speeds.
    /// </summary>
    [Theory]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Right)]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Down)]
    internal void CheckDirection_AtEveryOffsetAndSpeed_AgreesWithTheWalk(Direction direction)
    {
        // Arrange
        int disagreements = 0;

        // Act
        foreach (int velocity in TestData.Velocities)
        {
            for (int offsetY = -TestData.SweepReach; offsetY <= TestData.SweepReach; offsetY++)
            {
                for (int offsetX = -TestData.SweepReach; offsetX <= TestData.SweepReach; offsetX++)
                {
                    int walked = WalkedOneStepAtATime(direction, velocity,
                        TestData.MoverX, TestData.MoverY, TestData.MoverWidth, TestData.MoverHeight,
                        TestData.MoverX + offsetX, TestData.MoverY + offsetY,
                        TestData.CellSize, TestData.CellSize);

                    int checked_ = CollisionDetection.CheckDirection(direction, velocity,
                        TestData.MoverX, TestData.MoverY, TestData.MoverWidth, TestData.MoverHeight,
                        TestData.MoverX + offsetX, TestData.MoverY + offsetY,
                        TestData.CellSize, TestData.CellSize);

                    if (walked != checked_)
                    {
                        disagreements++;
                    }
                }
            }
        }

        // Assert
        Assert.Equal(0, disagreements);
    }

    /// <summary>
    ///     A mover that is not going anywhere. The walk never runs, so the answer is nought however
    ///     far away the other box is, and the skip must not turn that into the velocity.
    /// </summary>
    [Theory]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Right)]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Down)]
    internal void CheckDirection_AtNoVelocity_IsStillNought(Direction direction)
    {
        // Act
        int faraway = CollisionDetection.CheckDirection(direction, 0,
            TestData.MoverX, TestData.MoverY, TestData.MoverWidth, TestData.MoverHeight,
            TestData.FarAway, TestData.FarAway, TestData.CellSize, TestData.CellSize);

        // Assert
        Assert.Equal(0, faraway);
    }

    private static class TestData
    {
        public const int CellSize = 32;

        public const int PlayAreaWidth = 1024;

        public const int PlayAreaHeight = 576;

        /// <summary>
        ///     Blob's size, and a position well inside the screen so cells can sit on every side.
        /// </summary>
        public const int MoverWidth = 56;

        public const int MoverHeight = 56;

        public const int MoverX = 400;

        public const int MoverY = 300;

        public const int Velocity = 8;

        public const int FarAway = 900;

        /// <summary>
        ///     Far enough either way to cover the whole of the reach plus the fastest move, and a
        ///     margin past it where the answer must be the full velocity.
        /// </summary>
        public const int SweepReach = 80;

        public static readonly int[] Velocities = [1, 2, 8, 16];
    }
}
