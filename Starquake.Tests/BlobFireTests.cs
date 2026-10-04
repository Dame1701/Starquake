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
    // Internal rather than public, here and in the theories below that take one. The parameter
    // is one of the game's own types, and those are internal now - this project only sees them
    // through InternalsVisibleTo, which is not enough to let a public signature here name one.
    // Dropping to internal costs nothing: xunit requires the test class to be public, not its
    // test methods, so these are still found and still run.
    [Theory]
    [InlineData(Direction.Up, 100, 84)]
    [InlineData(Direction.Down, 100, 116)]
    internal void Advance_FiredAlongTheVerticalAxis_MovesThatWayAndRecordsTheDistance(
        Direction firedIn,
        int startY,
        int expectedY)
    {
        // Arrange
        var surroundings = new Surroundings();
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.MidScreenX, startY);

        // Act
        bool inFlight = shot.Advance(firedIn, TestData.FromShuttle, TestData.NotPointBlank);

        // Assert
        Assert.True(inFlight);
        Assert.Equal(TestData.MidScreenX, shot.X);
        Assert.Equal(expectedY, shot.Y);

        // It used to record nothing here: the distance counted the horizontal axis whichever way
        // the shot went, which is what left the point-blank check unable to apply to this one.
        Assert.Equal(TestData.Speed, shot.DistanceTravelled);
    }

    [Theory]
    [InlineData(Direction.Left, 100, 84)]
    [InlineData(Direction.Right, 100, 116)]
    internal void Advance_FiredAlongTheHorizontalAxis_MovesThatWayAndRecordsTheDistance(
        Direction firedIn,
        int startX,
        int expectedX)
    {
        // Arrange
        var surroundings = new Surroundings();
        var shot = new BlobFire(surroundings);
        shot.Launch(startX, TestData.MidScreenY);

        // Act
        bool inFlight = shot.Advance(firedIn, TestData.FromShuttle, TestData.NotPointBlank);

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
        var surroundings = new Surroundings
        {
            Clearance = 5
        };
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
        var surroundings = new Surroundings
        {
            Blocked = [Direction.Up]
        };
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.MidScreenX, TestData.MidScreenY);

        // Act
        bool inFlight = shot.Advance(Direction.Up, TestData.FromShuttle, TestData.NotPointBlank);

        // Assert
        Assert.True(inFlight);
        Assert.Equal(FireDeflection.Left, shot.Deflection);
        Assert.Equal(TestData.MidScreenX, shot.X);
        Assert.Equal(TestData.MidScreenY, shot.Y);
        (int X, int Y) sparks = Assert.Single(surroundings.Sparks);
        Assert.Equal((TestData.MidScreenX, TestData.MidScreenY + TestData.SparksYOffset), sparks);
    }

    [Fact]
    public void Advance_HitsSceneryOnFoot_DiesWithoutDeflecting()
    {
        // Arrange
        var surroundings = new Surroundings
        {
            Blocked = [Direction.Left]
        };
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.MidScreenX, TestData.MidScreenY);

        // Act
        bool inFlight = shot.Advance(Direction.Left, TestData.OnFoot, TestData.NotPointBlank);

        // Assert
        Assert.False(inFlight);
        Assert.Equal(FireDeflection.None, shot.Deflection);
        Assert.Single(surroundings.Sparks);
    }

    [Fact]
    public void Advance_AlreadyDeflected_TravelsBackAlongOneAxisAndOutAlongTheOther()
    {
        // Arrange
        var surroundings = new Surroundings
        {
            Blocked = [Direction.Up],
            Deflection = FireDeflection.Left
        };
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.MidScreenX, TestData.MidScreenY);
        shot.Advance(Direction.Up, TestData.FromShuttle, TestData.NotPointBlank);

        // Act
        bool inFlight = shot.Advance(Direction.Up, TestData.FromShuttle, TestData.NotPointBlank);

        // Assert
        Assert.True(inFlight);
        Assert.Equal(TestData.MidScreenX - TestData.Speed, shot.X);
        Assert.Equal(TestData.MidScreenY + TestData.Speed, shot.Y);
        Assert.Equal(TestData.Speed, shot.DistanceTravelled);
    }

    [Theory]
    [InlineData(Direction.Left, TestData.AtLowEdge, TestData.MidScreenY)]
    [InlineData(Direction.Right, TestData.AtHighEdgeX, TestData.MidScreenY)]
    [InlineData(Direction.Up, TestData.MidScreenX, TestData.AtLowEdge)]
    [InlineData(Direction.Down, TestData.MidScreenX, TestData.AtHighEdgeY)]
    internal void Advance_ReachesTheScreenEdgeTravellingStraight_DiesQuietly(Direction firedIn, int x, int y)
    {
        // Arrange
        var surroundings = new Surroundings();
        var shot = new BlobFire(surroundings);
        shot.Launch(x, y);

        // Act
        bool inFlight = shot.Advance(firedIn, TestData.FromShuttle, TestData.NotPointBlank);

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
        var surroundings = new Surroundings
        {
            Blocked = [Direction.Down],
            Deflection = FireDeflection.Right
        };
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.AtHighEdgeX, TestData.MidScreenY);
        shot.Advance(Direction.Down, TestData.FromShuttle, TestData.NotPointBlank);

        // Act
        bool inFlight = shot.Advance(Direction.Down, TestData.FromShuttle, TestData.NotPointBlank);

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
        var surroundings = new Surroundings
        {
            Blocked = [Direction.Right]
        };
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.AtLowEdge, TestData.MidScreenY);
        shot.Advance(Direction.Right, TestData.FromShuttle, TestData.NotPointBlank);

        // Act
        bool inFlight = shot.Advance(Direction.Right, TestData.FromShuttle, TestData.NotPointBlank);

        // Assert
        Assert.False(inFlight);
        Assert.Single(surroundings.Sparks);
    }

    /// <summary>
    ///     A shot that runs out of screen has not hit anything, and must not throw the sparks that
    ///     say it has. This held going right and not going left: the edge was measured as the
    ///     shot's <em>height</em> along both axes, which called a rightward shot spent thirty
    ///     pixels early and so hid the fault, while the leftward test asked for the shot to be past
    ///     nought - which it never is, because the collision map clamps it to the screen.
    /// </summary>
    [Theory]
    [InlineData(Direction.Left, TestData.AtLowEdge, TestData.MidScreenY)]
    [InlineData(Direction.Right, TestData.AtHighEdgeX, TestData.MidScreenY)]
    [InlineData(Direction.Up, TestData.MidScreenX, TestData.AtLowEdge)]
    [InlineData(Direction.Down, TestData.MidScreenX, TestData.AtHighEdgeY)]
    internal void Advance_RunningOutOfScreen_LeavesWithoutSparksWhicheverWayItWentOut(
        Direction firedIn,
        int x,
        int y)
    {
        // Arrange - nothing in the way, so only the edge can stop it
        var surroundings = new Surroundings();
        var shot = new BlobFire(surroundings);
        shot.Launch(x, y);

        // Act
        bool inFlight = shot.Advance(firedIn, TestData.FromShuttle, TestData.NotPointBlank);

        // Assert
        Assert.False(inFlight);
        Assert.Empty(surroundings.Sparks);
    }

    [Fact]
    public void Advance_FiredHorizontallyFromPointBlank_PushesThroughWhatWouldHaveStoppedIt()
    {
        // Arrange
        var surroundings = new Surroundings
        {
            Blocked = [Direction.Left]
        };
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.MidScreenX, TestData.MidScreenY);

        // Act
        bool inFlight = shot.Advance(Direction.Left, TestData.OnFoot, TestData.PointBlankReach);

        // Assert
        Assert.True(inFlight);
        Assert.Equal(TestData.MidScreenX - TestData.Speed, shot.X);
        Assert.Empty(surroundings.Sparks);
    }

    /// <summary>
    ///     The push-through used to be horizontal-only, so this shot deflected off the thing Blob
    ///     was standing on and threw sparks instead of leaving. Nothing about firing upwards makes
    ///     the item underneath him more of an obstacle, which is why the axis test went.
    /// </summary>
    [Theory]
    [InlineData(Direction.Up, 100, 84)]
    [InlineData(Direction.Down, 100, 116)]
    internal void Advance_FiredVerticallyFromPointBlank_PushesThroughWhatWouldHaveStoppedIt(
        Direction firedIn,
        int startY,
        int expectedY)
    {
        // Arrange
        var surroundings = new Surroundings
        {
            Blocked = [firedIn]
        };
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.MidScreenX, startY);

        // Act
        bool inFlight = shot.Advance(firedIn, TestData.FromShuttle, TestData.PointBlankReach);

        // Assert
        Assert.True(inFlight);
        Assert.Equal(expectedY, shot.Y);
        Assert.Equal(FireDeflection.None, shot.Deflection);
        Assert.Empty(surroundings.Sparks);
    }

    /// <summary>
    ///     The reach is a distance and not a licence: once the shot has gone further than the item
    ///     it was born inside, what is in its way counts again. Pinned on the vertical axis,
    ///     because that is the one that could not reach this code at all before.
    /// </summary>
    /// <remarks>
    ///     A blocked shot from a shuttle deflects rather than dying, and deflecting costs it a
    ///     frame in which it does not move - so the evidence that the obstacle is being felt again
    ///     is the deflection, the sparks, and the shot having stayed exactly where the first step
    ///     left it. On foot there is nothing to compare against: a vertical shot never leaves
    ///     Blob's hand at all.
    /// </remarks>
    [Fact]
    public void Advance_FiredVerticallyPastThePointBlankReach_FeelsWhatIsInItsWayAgain()
    {
        // Arrange - a reach of one step, so the second step is already beyond it
        var surroundings = new Surroundings
        {
            Blocked = [Direction.Up]
        };
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.MidScreenX, TestData.MidScreenY);

        // Act - the first step is point blank and pushes through
        shot.Advance(Direction.Up, TestData.FromShuttle, TestData.Speed);

        int afterThePushThrough = shot.Y;

        shot.Advance(Direction.Up, TestData.FromShuttle, TestData.Speed);

        // Assert
        Assert.Equal(TestData.MidScreenY - TestData.Speed, afterThePushThrough);
        Assert.Equal(afterThePushThrough, shot.Y);
        Assert.Equal(FireDeflection.Left, shot.Deflection);
        Assert.Single(surroundings.Sparks);
    }

    [Theory]
    [InlineData(Direction.None)]
    [InlineData(Direction.UpLeft)]
    [InlineData(Direction.Up)]
    internal void Advance_OnFootInADirectionItCannotTravel_DropsTheShot(Direction firedIn)
    {
        // Arrange
        var surroundings = new Surroundings();
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.MidScreenX, TestData.MidScreenY);

        // Act
        bool inFlight = shot.Advance(firedIn, TestData.OnFoot, TestData.NotPointBlank);

        // Assert
        Assert.False(inFlight);
        Assert.Empty(surroundings.Sparks);
    }

    [Theory]
    [InlineData(Direction.None)]
    [InlineData(Direction.DownRight)]
    internal void Advance_FromAShuttleInADirectionItCannotTravel_LeavesTheShotWhereItIs(Direction firedIn)
    {
        // Arrange
        var surroundings = new Surroundings();
        var shot = new BlobFire(surroundings);
        shot.Launch(TestData.MidScreenX, TestData.MidScreenY);

        // Act
        bool inFlight = shot.Advance(firedIn, TestData.FromShuttle, TestData.NotPointBlank);

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
        var surroundings = new Surroundings
        {
            Blocked = [Direction.Left]
        };
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

        /// <summary>
        ///     How far the shot may travel in any direction that is not in <see cref="Blocked" />.
        /// </summary>
        public int Clearance { get; init; } = TestData.Speed;

        public HashSet<Direction> Blocked { get; init; } = [];

        public FireDeflection Deflection { get; init; } = FireDeflection.Left;

        public int ScreenWidth => TestData.ScreenWidth;

        public int ScreenHeight => TestData.ScreenHeight;

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

        /// <summary>
        ///     The shot's box is <see cref="Width" /> across and <see cref="Length" /> down, which
        ///     is why the two high-edge constants below do not use the same one.
        /// </summary>
        public const int Width = 2;

        public const int MidScreenX = 100;

        public const int MidScreenY = 100;

        /// <summary>
        ///     Hard against the low end of either axis, which is as far as a shot gets: the
        ///     collision map clamps anything that is not Blob to the screen, so one travelling that
        ///     way stops at nought rather than passing it.
        /// </summary>
        public const int AtLowEdge = 0;

        /// <summary>
        ///     Far enough along that the shot's far end has reached the screen, which is what the
        ///     high-end boundary tests, rather than its origin. Measured with the extent along the
        ///     axis being travelled - across for the one, down for the other - which this used to
        ///     get wrong on both counts.
        /// </summary>
        public const int AtHighEdgeX = ScreenWidth - Width;

        /// <inheritdoc cref="AtHighEdgeX" />
        public const int AtHighEdgeY = ScreenHeight - Length;

        public const bool FromShuttle = true;

        public const bool OnFoot = false;

        public static readonly int? NotPointBlank = null;

        /// <summary>Further than any shot in these tests travels, so the push-through always applies.</summary>
        public static readonly int? PointBlankReach = 1000;
    }
}
