// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Collisions;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     <see cref="CollisionDetection" /> is the primitive every moving thing in the game is
///     stepped by, so these pin what it currently decides rather than what a rectangle overlap
///     test ideally would. The two differ: it compares centre distances with truncating integer
///     division, so boxes of odd size can overlap by a pixel and not be counted as touching.
/// </summary>
public class CollisionDetectionTests
{
    [Fact]
    public void Check_BoxesInTheSamePlace_Collide()
    {
        // Arrange
        var mover = TestData.BoxAtOrigin;
        var obstacle = TestData.BoxAtOrigin;

        // Act
        var collided = CollisionDetection.Check(mover, obstacle);

        // Assert
        Assert.True(collided);
    }

    [Fact]
    public void Check_BoxesOverlappingByOnePixel_Collide()
    {
        // Arrange
        var obstacleOverlappingByOnePixel = new Rectangle(TestData.BoxSize - 1, 0, TestData.BoxSize, TestData.BoxSize);

        // Act
        var collided = CollisionDetection.Check(TestData.BoxAtOrigin, obstacleOverlappingByOnePixel);

        // Assert
        Assert.True(collided);
    }

    [Fact]
    public void Check_BoxesTouchingEdgeToEdge_DoNotCollide()
    {
        // Arrange
        var obstacleAgainstTheRightEdge = new Rectangle(TestData.BoxSize, 0, TestData.BoxSize, TestData.BoxSize);

        // Act
        var collided = CollisionDetection.Check(TestData.BoxAtOrigin, obstacleAgainstTheRightEdge);

        // Assert
        Assert.False(collided);
    }

    [Fact]
    public void Check_BoxesOverlappingInXOnly_DoNotCollide()
    {
        // Arrange
        var obstacleFarBelow = new Rectangle(0, TestData.WellClear, TestData.BoxSize, TestData.BoxSize);

        // Act
        var collided = CollisionDetection.Check(TestData.BoxAtOrigin, obstacleFarBelow);

        // Assert
        Assert.False(collided);
    }

    [Fact]
    public void Check_BoxesWellApart_DoNotCollide()
    {
        // Arrange
        var obstacleFarAway = new Rectangle(TestData.WellClear, TestData.WellClear, TestData.BoxSize, TestData.BoxSize);

        // Act
        var collided = CollisionDetection.Check(TestData.BoxAtOrigin, obstacleFarAway);

        // Assert
        Assert.False(collided);
    }

    [Fact]
    public void Check_OddSizedBoxesOverlappingByOnePixel_DoNotCollide()
    {
        // Arrange
        const int oddSize = 15;
        var halvesLoseTheOverlap = new Rectangle(oddSize - 1, 0, oddSize, oddSize);

        // Act
        var collided = CollisionDetection.Check(new Rectangle(0, 0, oddSize, oddSize), halvesLoseTheOverlap);

        // Assert
        Assert.False(collided);
    }

    [Theory]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Down)]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Right)]
    public void CheckDirection_NothingInTheWay_AllowsTheFullVelocity(Direction direction)
    {
        // Arrange
        var obstacleWellClear = new Rectangle(TestData.WellClear, TestData.WellClear, TestData.BoxSize, TestData.BoxSize);

        // Act
        var allowed = CollisionDetection.CheckDirection(
            direction,
            TestData.Velocity,
            0, 0, TestData.BoxSize, TestData.BoxSize,
            obstacleWellClear.X, obstacleWellClear.Y, obstacleWellClear.Width, obstacleWellClear.Height);

        // Assert
        Assert.Equal(TestData.Velocity, allowed);
    }

    [Theory]
    [InlineData(Direction.Up, 0, TestData.GapBeforeObstacle, 0, 0)]
    [InlineData(Direction.Down, 0, 0, 0, TestData.GapBeforeObstacle)]
    [InlineData(Direction.Left, TestData.GapBeforeObstacle, 0, 0, 0)]
    [InlineData(Direction.Right, 0, 0, TestData.GapBeforeObstacle, 0)]
    public void CheckDirection_ObstacleWithinTheVelocity_AllowsOnlyTheGap(
        Direction direction,
        int moverX,
        int moverY,
        int obstacleX,
        int obstacleY)
    {
        // Act
        var allowed = CollisionDetection.CheckDirection(
            direction,
            TestData.Velocity,
            moverX, moverY, TestData.BoxSize, TestData.BoxSize,
            obstacleX, obstacleY, TestData.BoxSize, TestData.BoxSize);

        // Assert
        Assert.Equal(TestData.GapBeforeObstacle - TestData.BoxSize, allowed);
    }

    [Fact]
    public void CheckDirection_ObstacleAlreadyTouching_AllowsNothing()
    {
        // Arrange
        const int moverY = TestData.BoxSize;

        // Act
        var allowed = CollisionDetection.CheckDirection(
            Direction.Up,
            TestData.Velocity,
            0, moverY, TestData.BoxSize, TestData.BoxSize,
            0, 0, TestData.BoxSize, TestData.BoxSize);

        // Assert
        Assert.Equal(0, allowed);
    }

    [Theory]
    [InlineData(Direction.None)]
    [InlineData(Direction.UpLeft)]
    [InlineData(Direction.DownRight)]
    public void CheckDirection_DirectionItDoesNotStepIn_AllowsNothing(Direction direction)
    {
        // Arrange
        var obstacleWellClear = new Rectangle(TestData.WellClear, TestData.WellClear, TestData.BoxSize, TestData.BoxSize);

        // Act
        var allowed = CollisionDetection.CheckDirection(
            direction,
            TestData.Velocity,
            0, 0, TestData.BoxSize, TestData.BoxSize,
            obstacleWellClear.X, obstacleWellClear.Y, obstacleWellClear.Width, obstacleWellClear.Height);

        // Assert
        Assert.Equal(0, allowed);
    }

    private static class TestData
    {
        public const int BoxSize = 16;

        public const int Velocity = 8;

        /// <summary>
        ///     Far enough that nothing the tests do brings the two boxes near each other.
        /// </summary>
        public const int WellClear = 100;

        /// <summary>
        ///     Where an obstacle is put so that it is inside the velocity but not yet touching. The
        ///     mover gets <see cref="GapBeforeObstacle" /> minus <see cref="BoxSize" /> of movement.
        /// </summary>
        public const int GapBeforeObstacle = 20;

        public static readonly Rectangle BoxAtOrigin = new(0, 0, BoxSize, BoxSize);
    }
}
