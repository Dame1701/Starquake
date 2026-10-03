// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.ActiveSprites;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.SharedLibs;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The lift that carries Blob up a shaft: when he is on one, how far it takes him, and when he is
///     allowed to step off sideways. All of it was inside <c>Blob.GoUpTheShaft</c>, which needed Blob,
///     and Blob needs the content pipeline and a graphics device.
/// </summary>
/// <remarks>
///     <see cref="TunnelLift" /> takes its sound as two callbacks rather than a sound engine, which is
///     what makes this reachable - the same trade <see cref="BlobFall" /> makes by returning a flag. The
///     callbacks are counted here, because when the lift's sound starts and stops is part of what it
///     decides.
/// </remarks>
public class TunnelLiftTests
{
    [Fact]
    public void Step_NothingOverhead_DoesNotCarryHim()
    {
        // Arrange
        var sounds = new SoundLog();
        TunnelLift lift = sounds.Lift();

        // Act
        int risen = lift.Step(MapOf(TestData.EmptyScreen()), TestData.BlobAt(0, 300), null, false, false,
            TestData.WalkSpeed);

        // Assert
        Assert.Equal(0, risen);
        Assert.False(lift.GoingUp);
        Assert.Equal(0, sounds.Started);
    }

    [Fact]
    public void Step_UnderTheMiddleOfAShaft_PutsHimOnAndCarriesHimUp()
    {
        // Arrange
        var sounds = new SoundLog();
        TunnelLift lift = sounds.Lift();

        // Act
        int risen = lift.Step(MapOf(TestData.EmptyScreen()), TestData.BlobAt(0, 300),
            TestData.Overhead(ActionTiles.TunnelLiftShaft1, TestData.ShaftCentre, 0),
            false, false, TestData.WalkSpeed);

        // Assert
        Assert.Equal(TestData.LiftSpeed, risen);
        Assert.True(lift.GoingUp);
        Assert.Equal(1, sounds.Started);
    }

    /// <summary>
    ///     Walking past the edge of a shaft must not grab him, or a lift would catch anyone who brushed it.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    [InlineData(64)]
    public void Step_UnderTheEdgeOfAShaft_LeavesHimAlone(int xInsideTheTile)
    {
        // Arrange
        var sounds = new SoundLog();
        TunnelLift lift = sounds.Lift();

        // Act
        int risen = lift.Step(MapOf(TestData.EmptyScreen()), TestData.BlobAt(0, 300),
            TestData.Overhead(ActionTiles.TunnelLiftShaft1, xInsideTheTile, 0),
            false, false, TestData.WalkSpeed);

        // Assert
        Assert.Equal(0, risen);
        Assert.False(lift.GoingUp);
    }

    [Theory]
    [InlineData(ActionTiles.TunnelLiftShaft1)]
    [InlineData(ActionTiles.TunnelLiftShaft2)]
    [InlineData(ActionTiles.TunnelLiftShaft3)]
    [InlineData(ActionTiles.TunnelLiftTile)]
    internal void Step_AnyOfTheShaftTiles_WillCarryHim(ActionTiles shaft)
    {
        // Arrange
        var sounds = new SoundLog();
        TunnelLift lift = sounds.Lift();

        // Act
        int risen = lift.Step(MapOf(TestData.EmptyScreen()), TestData.BlobAt(0, 300),
            TestData.Overhead(shaft, TestData.ShaftCentre, 0), false, false, TestData.WalkSpeed);

        // Assert
        Assert.Equal(TestData.LiftSpeed, risen);
    }

    [Theory]
    [InlineData(ActionTiles.TunnelLiftTop1)]
    [InlineData(ActionTiles.TunnelLiftTop2)]
    [InlineData(ActionTiles.TunnelLiftTop3)]
    internal void Step_ReachingTheTopOfTheShaft_StopsHimGoingUp(ActionTiles top)
    {
        // Arrange - already going up
        var sounds = new SoundLog();
        TunnelLift lift = sounds.Lift();
        ScreenCollisionMap map = MapOf(TestData.EmptyScreen());
        lift.Step(map, TestData.BlobAt(0, 300), TestData.InsideAShaft(), false, false, TestData.WalkSpeed);

        // Act
        int risen = lift.Step(map, TestData.BlobAt(0, 292),
            TestData.Overhead(top, 0, TestData.ShaftTop), false, false, TestData.WalkSpeed);

        // Assert
        Assert.Equal(0, risen);
        Assert.False(lift.GoingUp);
    }

    /// <summary>
    ///     <see cref="ActionTiles.TunnelLiftTopAlternate" /> is documented as a fourth lift top, and both
    ///     the renderer and the animation engine treat it as one - but the way up never has, and this pins
    ///     that as it was found rather than quietly changing it.
    /// </summary>
    /// <remarks>
    ///     It cannot bite on the shipped map: tile 396 is placed fifteen times across nine screens and is
    ///     never directly above a shaft tile, so it never caps one. Were it ever authored as the top of a
    ///     shaft, he would carry on past it. Left alone because a move is the wrong commit to change
    ///     behaviour in, and because the screen editor could make it reachable tomorrow - see
    ///     <c>GODCLASSES.md</c>.
    /// </remarks>
    [Fact]
    public void Step_ReachingTheAlternateTop_DoesNotStopHimGoingUp_AsItNeverDid()
    {
        // Arrange - already going up
        var sounds = new SoundLog();
        TunnelLift lift = sounds.Lift();
        ScreenCollisionMap map = MapOf(TestData.EmptyScreen());
        lift.Step(map, TestData.BlobAt(0, 300), TestData.InsideAShaft(), false, false, TestData.WalkSpeed);

        // Act
        int risen = lift.Step(map, TestData.BlobAt(0, 292),
            TestData.Overhead(ActionTiles.TunnelLiftTopAlternate, 0, TestData.ShaftTop),
            false, false, TestData.WalkSpeed);

        // Assert
        Assert.Equal(TestData.LiftSpeed, risen);
        Assert.True(lift.GoingUp);
    }

    /// <summary>
    ///     He may not step off into the passage he arrived through, which is what the minimum distance is for -
    ///     one frame's travel is not enough.
    /// </summary>
    [Fact]
    public void Step_SteeredOutOfTheShaftTooSoon_KeepsCarryingHim()
    {
        // Arrange - a passage to his left, and only one frame of travel behind him
        var sounds = new SoundLog();
        TunnelLift lift = sounds.Lift();
        ScreenCollisionMap map = MapOf(TestData.EmptyScreen());
        lift.Step(map, TestData.BlobAt(TestData.MidScreenX, 300), TestData.InsideAShaft(), false, false,
            TestData.WalkSpeed);

        // Act
        int risen = lift.Step(map, TestData.BlobAt(TestData.MidScreenX, 292), TestData.AtASidePassage(),
            true, false, TestData.WalkSpeed);

        // Assert
        Assert.Equal(TestData.LiftSpeed, risen);
        Assert.True(lift.GoingUp);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Step_SteeredOutOfTheShaftAfterGoingUpFarEnough_StepsHimOffAndStopsTheSound(bool left, bool right)
    {
        // Arrange - far enough up to clear the minimum
        var sounds = new SoundLog();
        TunnelLift lift = sounds.Lift();
        ScreenCollisionMap map = MapOf(TestData.EmptyScreen());
        TestData.GoUpFor(lift, map, frames: 3);

        // Act
        int risen = lift.Step(map, TestData.BlobAt(TestData.MidScreenX, 200), TestData.AtASidePassage(),
            left, right, TestData.WalkSpeed);

        // Assert
        Assert.Equal(0, risen);
        Assert.False(lift.GoingUp);
        Assert.Equal(1, sounds.Stopped);
    }

    /// <summary>
    ///     Steering into a wall is not a way off: the passage has to be somewhere he could actually walk.
    /// </summary>
    [Fact]
    public void Step_SteeredIntoAWallAfterGoingUpFarEnough_KeepsCarryingHim()
    {
        // Arrange - a solid column to his left, so there is no room that way
        GameScreenData screen = TestData.EmptyScreen();

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            screen.ScreenTiles[0, y] = new ScreenTile(TestData.SolidTile);
        }

        var sounds = new SoundLog();
        TunnelLift lift = sounds.Lift();
        ScreenCollisionMap map = MapOf(screen);
        TestData.GoUpFor(lift, map, frames: 3, xPos: TileMetrics.TileWidth);

        // Act
        int risen = lift.Step(map, TestData.BlobAt(TileMetrics.TileWidth, 200), TestData.AtASidePassage(),
            true, false, TestData.WalkSpeed);

        // Assert
        Assert.Equal(TestData.LiftSpeed, risen);
        Assert.True(lift.GoingUp);
    }

    /// <summary>
    ///     The minimum distance counts from getting on, so a second shaft starts the count again.
    ///     Without this, having gone up one shaft, Blob could step straight out of the next one he
    ///     entered.
    /// </summary>
    [Fact]
    public void Step_GettingOnASecondShaft_CountsTheDistanceFromScratch()
    {
        // Arrange - a long way up, off the top, then back onto another shaft
        var sounds = new SoundLog();
        TunnelLift lift = sounds.Lift();
        ScreenCollisionMap map = MapOf(TestData.EmptyScreen());
        TestData.GoUpFor(lift, map, frames: 4);

        lift.Step(map, TestData.BlobAt(TestData.MidScreenX, 200),
            TestData.Overhead(ActionTiles.TunnelLiftTop1, 0, TestData.ShaftTop), false, false,
            TestData.WalkSpeed);
        Assert.False(lift.GoingUp);

        lift.Step(map, TestData.BlobAt(TestData.MidScreenX, 200), TestData.InsideAShaft(), false, false,
            TestData.WalkSpeed);

        // Act
        int risen = lift.Step(map, TestData.BlobAt(TestData.MidScreenX, 192), TestData.AtASidePassage(),
            true, false, TestData.WalkSpeed);

        // Assert
        Assert.Equal(TestData.LiftSpeed, risen);
        Assert.True(lift.GoingUp);
    }

    [Fact]
    public void LetGo_WhileGoingUp_TakesHimOffWithoutStepping()
    {
        // Arrange
        var sounds = new SoundLog();
        TunnelLift lift = sounds.Lift();
        ScreenCollisionMap map = MapOf(TestData.EmptyScreen());
        lift.Step(map, TestData.BlobAt(0, 300), TestData.InsideAShaft(), false, false, TestData.WalkSpeed);

        // Act
        lift.LetGo();

        // Assert
        Assert.False(lift.GoingUp);
        Assert.Equal(1, sounds.Started);
        Assert.Equal(0, sounds.Stopped);
    }

    private static ScreenCollisionMap MapOf(GameScreenData screen)
    {
        var map = new ScreenCollisionMap([screen], TestData.TileCollisionData());
        map.Rebuild(0);

        return map;
    }

    /// <summary>
    ///     Counts the two sound calls the lift is handed, since when they fire is part of the behaviour.
    /// </summary>
    private sealed class SoundLog
    {
        public int Started { get; private set; }

        public int Stopped { get; private set; }

        public TunnelLift Lift() => new(() => Started++, () => Stopped++);
    }

    private static class TestData
    {
        public const int SolidTile = 1;

        public const int LiftSpeed = 8;

        public const int WalkSpeed = 8;

        /// <summary>Across the middle of the tile overhead, which is the only place a shaft grabs him.</summary>
        public const int ShaftCentre = 32;

        /// <summary>A tile's height into the tile overhead, where the top and a side passage are read.</summary>
        public const int ShaftTop = 96;

        /// <summary>Clear of the left-hand edge, so a test about walls places its own.</summary>
        public const int MidScreenX = TileMetrics.TileWidth * 3;

        public static GameScreenData EmptyScreen()
            => new(TileMetrics.TilesAcross, TileMetrics.TilesDown);

        public static Rectangle BlobAt(int xPos, int yPos)
            => new(xPos, yPos, SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        public static TileDetected Overhead(ActionTiles tile, int xInsideTheTile, int yInsideTheTile)
            => new((int)tile, xInsideTheTile, yInsideTheTile, 0, 0);

        /// <summary>Overhead reading that puts him in a shaft and not at a way out of it.</summary>
        public static TileDetected InsideAShaft()
            => Overhead(ActionTiles.TunnelLiftShaft1, ShaftCentre, 0);

        /// <summary>Overhead reading that offers him a way out sideways.</summary>
        public static TileDetected AtASidePassage()
            => Overhead(ActionTiles.TunnelLiftShaft1, ShaftCentre, ShaftTop);

        public static void GoUpFor(TunnelLift lift, ScreenCollisionMap map, int frames, int xPos = MidScreenX)
        {
            int yPos = 300;

            for (int frame = 0; frame < frames; frame++)
            {
                yPos -= lift.Step(map, BlobAt(xPos, yPos), InsideAShaft(), false, false, WalkSpeed);
            }
        }

        public static int[,,] TileCollisionData()
        {
            var data = new int[
                SolidTile + 1,
                TileMetrics.CollisionCellsAcrossTile,
                TileMetrics.CollisionCellsDownTile];

            for (int across = 0; across < TileMetrics.CollisionCellsAcrossTile; across++)
            {
                for (int down = 0; down < TileMetrics.CollisionCellsDownTile; down++)
                {
                    data[SolidTile, across, down] = 1;
                }
            }

            return data;
        }
    }
}
