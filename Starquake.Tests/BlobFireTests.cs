// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.ActiveSprites;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     <see cref="BlobFire" /> replaced six near-identical branches, two of which had silently
///     drifted from the other four - one testing a Y coordinate against the screen's width, the
///     other forgetting to record that it had reached the screen's edge at all. Neither showed up
///     in a build and neither is obvious in play, so the cases that pin the axes and the edges
///     down are the point of this file.
/// </summary>
public class BlobFireTests
{
    [Theory]
    [InlineData(Direction.Up, 100, 84)]
    [InlineData(Direction.Down, 100, 116)]
    public void Advance_FiredAlongTheVerticalAxis_MovesThatWayAndRecordsNoDistance(
        Direction firedIn,
        int startY,
        int expectedY)
    {
        // Arrange
        var surroundings = new Surroundings();
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.MidScreenX, startY);

        // Act
        var inFlight = shot.Advance(firedIn, TestData.FromShuttle, TestData.NotPointBlank);

        // Assert
        Assert.True(inFlight);
        Assert.Equal(TestData.MidScreenX, shot.X);
        Assert.Equal(expectedY, shot.Y);
        Assert.Equal(0, shot.DistanceTravelled);
    }

    [Theory]
    [InlineData(Direction.Left, 100, 84)]
    [InlineData(Direction.Right, 100, 116)]
    public void Advance_FiredAlongTheHorizontalAxis_MovesThatWayAndRecordsTheDistance(
        Direction firedIn,
        int startX,
        int expectedX)
    {
        // Arrange
        var surroundings = new Surroundings();
        var shot = new BlobFire(surroundings);
        shot.Launch(startX, TestData.MidScreenY);

        // Act
        var inFlight = shot.Advance(firedIn, TestData.FromShuttle, TestData.NotPointBlank);

        // Assert
        Assert.True(inFlight);
        Assert.Equal(expectedX, shot.X);
        Assert.Equal(TestData.MidScreenY, shot.Y);
        Assert.Equal(TestData.Speed, shot.DistanceTravelled);
    }

    [Fact]
    public void Advance_ScenerylessThanAFullStepAway_MovesOnlyAsFarAsItCan()
    {
        // Arrange
        var surroundings = new Surroundings { Clearance = 5 };
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.MidScreenX, TestData.MidScreenY);

        // Act
        shot.Advance(Direction.Right, TestData.FromShuttle, TestData.NotPointBlank);

        // Assert
        Assert.Equal(TestData.MidScreenX + 5, shot.X);
        Assert.Equal(5, shot.DistanceTravelled);
    }

    [Fact]
    public void Advance_HitsSceneryFromAShuttle_DeflectsAndThrowsSparksWithoutMoving()
    {
        // Arrange
        var surroundings = new Surroundings { Blocked = [Direction.Up] };
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.MidScreenX, TestData.MidScreenY);

        // Act
        var inFlight = shot.Advance(Direction.Up, TestData.FromShuttle, TestData.NotPointBlank);

        // Assert
        Assert.True(inFlight);
        Assert.Equal(FireDeflection.Left, shot.Deflection);
        Assert.Equal(TestData.MidScreenX, shot.X);
        Assert.Equal(TestData.MidScreenY, shot.Y);
        var sparks = Assert.Single(surroundings.Sparks);
        Assert.Equal((TestData.MidScreenX, TestData.MidScreenY + TestData.SparksYOffset), sparks);
    }

    [Fact]
    public void Advance_HitsSceneryOnFoot_DiesWithoutDeflecting()
    {
        // Arrange
        var surroundings = new Surroundings { Blocked = [Direction.Left] };
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.MidScreenX, TestData.MidScreenY);

        // Act
        var inFlight = shot.Advance(Direction.Left, TestData.OnFoot, TestData.NotPointBlank);

        // Assert
        Assert.False(inFlight);
        Assert.Equal(FireDeflection.None, shot.Deflection);
        Assert.Single(surroundings.Sparks);
    }

    [Fact]
    public void Advance_AlreadyDeflected_TravelsBackAlongOneAxisAndOutAlongTheOther()
    {
        // Arrange
        var surroundings = new Surroundings { Blocked = [Direction.Up], Deflection = FireDeflection.Left };
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.MidScreenX, TestData.MidScreenY);
        shot.Advance(Direction.Up, TestData.FromShuttle, TestData.NotPointBlank);

        // Act
        var inFlight = shot.Advance(Direction.Up, TestData.FromShuttle, TestData.NotPointBlank);

        // Assert
        Assert.True(inFlight);
        Assert.Equal(TestData.MidScreenX - TestData.Speed, shot.X);
        Assert.Equal(TestData.MidScreenY + TestData.Speed, shot.Y);
        Assert.Equal(TestData.Speed, shot.DistanceTravelled);
    }

    [Theory]
    [InlineData(Direction.Left, TestData.PastLowEdge, TestData.MidScreenY)]
    [InlineData(Direction.Right, TestData.AtHighEdgeX, TestData.MidScreenY)]
    [InlineData(Direction.Up, TestData.MidScreenX, TestData.PastLowEdge)]
    [InlineData(Direction.Down, TestData.MidScreenX, TestData.AtHighEdgeY)]
    public void Advance_ReachesTheScreenEdgeTravellingStraight_DiesQuietly(Direction firedIn, int x, int y)
    {
        // Arrange
        var surroundings = new Surroundings();
        var shot = new BlobFire(surroundings);
        shot.Launch(x, y);

        // Act
        var inFlight = shot.Advance(firedIn, TestData.FromShuttle, TestData.NotPointBlank);

        // Assert
        Assert.False(inFlight);
        Assert.Empty(surroundings.Sparks);
        Assert.Equal(x, shot.X);
        Assert.Equal(y, shot.Y);
    }

    [Fact]
    public void Advance_DeflectedRightAfterBeingFiredDown_StopsAtTheRightHandEdge()
    {
        // Arrange
        var surroundings = new Surroundings { Blocked = [Direction.Down], Deflection = FireDeflection.Right };
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.AtHighEdgeX, TestData.MidScreenY);
        shot.Advance(Direction.Down, TestData.FromShuttle, TestData.NotPointBlank);

        // Act
        var inFlight = shot.Advance(Direction.Down, TestData.FromShuttle, TestData.NotPointBlank);

        // Assert
        Assert.False(inFlight);
        Assert.Equal(TestData.AtHighEdgeX, shot.X);
        Assert.Equal(TestData.MidScreenY, shot.Y);
        Assert.Single(surroundings.Sparks);
    }

    [Fact]
    public void Advance_DeflectedAfterBeingFiredRight_StopsQuietlyAtTheLeftHandEdge()
    {
        // Arrange
        var surroundings = new Surroundings { Blocked = [Direction.Right] };
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.PastLowEdge, TestData.MidScreenY);
        shot.Advance(Direction.Right, TestData.FromShuttle, TestData.NotPointBlank);

        // Act
        var inFlight = shot.Advance(Direction.Right, TestData.FromShuttle, TestData.NotPointBlank);

        // Assert
        Assert.False(inFlight);
        Assert.Single(surroundings.Sparks);
    }

    [Fact]
    public void Advance_FiredHorizontallyFromPointBlank_PushesThroughWhatWouldHaveStoppedIt()
    {
        // Arrange
        var surroundings = new Surroundings { Blocked = [Direction.Left] };
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.MidScreenX, TestData.MidScreenY);

        // Act
        var inFlight = shot.Advance(Direction.Left, TestData.OnFoot, TestData.PointBlankReach);

        // Assert
        Assert.True(inFlight);
        Assert.Equal(TestData.MidScreenX - TestData.Speed, shot.X);
        Assert.Empty(surroundings.Sparks);
    }

    [Fact]
    public void Advance_FiredVerticallyFromPointBlank_IsStoppedAnyway()
    {
        // Arrange
        var surroundings = new Surroundings { Blocked = [Direction.Up] };
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.MidScreenX, TestData.MidScreenY);

        // Act
        shot.Advance(Direction.Up, TestData.FromShuttle, TestData.PointBlankReach);

        // Assert
        Assert.Equal(FireDeflection.Left, shot.Deflection);
        Assert.Single(surroundings.Sparks);
    }

    [Theory]
    [InlineData(Direction.None)]
    [InlineData(Direction.UpLeft)]
    [InlineData(Direction.Up)]
    public void Advance_OnFootInADirectionItCannotTravel_DropsTheShot(Direction firedIn)
    {
        // Arrange
        var surroundings = new Surroundings();
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.MidScreenX, TestData.MidScreenY);

        // Act
        var inFlight = shot.Advance(firedIn, TestData.OnFoot, TestData.NotPointBlank);

        // Assert
        Assert.False(inFlight);
        Assert.Empty(surroundings.Sparks);
    }

    [Theory]
    [InlineData(Direction.None)]
    [InlineData(Direction.DownRight)]
    public void Advance_FromAShuttleInADirectionItCannotTravel_LeavesTheShotWhereItIs(Direction firedIn)
    {
        // Arrange
        var surroundings = new Surroundings();
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.MidScreenX, TestData.MidScreenY);

        // Act
        var inFlight = shot.Advance(firedIn, TestData.FromShuttle, TestData.NotPointBlank);

        // Assert
        Assert.True(inFlight);
        Assert.Equal(TestData.MidScreenX, shot.X);
        Assert.Equal(TestData.MidScreenY, shot.Y);
        Assert.Empty(surroundings.Sparks);
    }

    [Fact]
    public void Launch_AfterAShotHasTravelledAndDeflected_StartsTheNextOneClean()
    {
        // Arrange
        var surroundings = new Surroundings { Blocked = [Direction.Left] };
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.MidScreenX, TestData.MidScreenY);
        shot.Advance(Direction.Left, TestData.FromShuttle, TestData.NotPointBlank);

        // Act
        shot.Launch(TestData.MidScreenX, TestData.MidScreenY);

        // Assert
        Assert.Equal(FireDeflection.None, shot.Deflection);
        Assert.Equal(0, shot.DistanceTravelled);
    }

    private sealed class Surroundings : IFireSurroundings
    {
        public List<(int X, int Y)> Sparks { get; } = [];

        public int ScreenWidth => TestData.ScreenWidth;

        public int ScreenHeight => TestData.ScreenHeight;

        /// <summary>
        ///     How far the shot may travel in any direction that is not in <see cref="Blocked" />.
        /// </summary>
        public int Clearance { get; init; } = TestData.Speed;

        public HashSet<Direction> Blocked { get; init; } = [];

        public FireDeflection Deflection { get; init; } = FireDeflection.Left;

        public int DistanceClear(Direction direction, int x, int y, int width, int height, int distance)
            => Blocked.Contains(direction) ? 0 : Clearance;

        public FireDeflection ChooseDeflection() => Deflection;

        public void ThrowSparks(int x, int y) => Sparks.Add((x, y));
    }

    private static class TestData
    {
        public const int ScreenWidth = 256;

        public const int ScreenHeight = 192;

        /// <summary>Kept in step with <c>BlobFire</c>'s own constants.</summary>
        public const int Speed = 16;

        /// <inheritdoc cref="Speed" />
        public const int SparksYOffset = 24;

        /// <inheritdoc cref="Speed" />
        public const int Length = 32;

        public const int MidScreenX = 100;

        public const int MidScreenY = 100;

        /// <summary>Already off the low end of either axis, so any shot heading that way is spent.</summary>
        public const int PastLowEdge = -1;

        /// <summary>
        ///     Far enough along that the shot's far end is past the screen, which is what the high-end
        ///     boundary tests, rather than its origin.
        /// </summary>
        public const int AtHighEdgeX = ScreenWidth - Length + 1;

        /// <inheritdoc cref="AtHighEdgeX" />
        public const int AtHighEdgeY = ScreenHeight - Length + 1;

        public const bool FromShuttle = true;

        public const bool OnFoot = false;

        public static readonly int? NotPointBlank = null;

        /// <summary>Further than any shot in these tests travels, so the push-through always applies.</summary>
        public static readonly int? PointBlankReach = 1000;
    }
}
