// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.ActiveSprites;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.Core;
using Starquake.SharedLibs;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     Fall damage arithmetic: how fast Blob falls, how far he has come, and when the fall is worth
///     hearing.
/// </summary>
/// <remarks>
///     <see cref="BlobFall" /> needs a <see cref="ScreenCollisionMap" /> and nothing else. The
///     falling sound is the caller's to play, so the fall itself only has to say whether the drop
///     has become audible.
/// </remarks>
public class BlobFallTests
{
    [Fact]
    public void Step_InOpenAir_FallsAtTheStartingSpeed()
    {
        // Arrange
        var fall = new BlobFall();
        ScreenCollisionMap map = MapOf(TestData.EmptyScreen());

        // Act
        Fall first = fall.Step(map, TestData.BlobAt(0, 0), false, false);

        // Assert
        Assert.Equal(TestData.StartingFallSpeed, first.Distance);
        Assert.Equal(TestData.StartingFallSpeed, fall.Height);
        Assert.False(first.OnTheGround);
    }

    [Fact]
    public void Step_StandingOnSolidGround_DoesNotFallAndSaysSo()
    {
        // Arrange - a floor directly under his feet
        var fall = new BlobFall();
        ScreenCollisionMap map = MapOf(TestData.ScreenWithAFloorInRow(1));

        // Act
        Fall landed = fall.Step(map, TestData.BlobAt(0, TestData.StandingOnRow1Y), false, false);

        // Assert
        Assert.Equal(0, landed.Distance);
        Assert.True(landed.OnTheGround);
        Assert.Equal(0, fall.Height);
    }

    /// <summary>
    ///     What makes a long drop feel like one. Blob gains a pixel a frame for every 12 pixels
    ///     fallen, so the fourth frame of a fall is faster than the first three.
    /// </summary>
    [Fact]
    public void Step_FallingPastTheSpeedUpDistance_GainsAPixelAFrame()
    {
        // Arrange
        var fall = new BlobFall();
        ScreenCollisionMap map = MapOf(TestData.EmptyScreen());

        // Act
        int[] distances = TestData.DropFor(fall, map, 4);

        // Assert
        Assert.Equal(
            new[]
            {
                TestData.StartingFallSpeed,
                TestData.StartingFallSpeed,
                TestData.StartingFallSpeed
            },
            distances[..3]);
        Assert.Equal(TestData.StartingFallSpeed + 1, distances[3]);
    }

    [Fact]
    public void Step_FallingAllTheWayDown_NeverExceedsTheTerminalSpeed()
    {
        // Arrange
        var fall = new BlobFall();
        ScreenCollisionMap map = MapOf(TestData.EmptyScreen());

        // Act. Far further than any screen is tall, so the speed has every chance to run away.
        int[] distances = TestData.DropFor(fall, map, 200);

        // Assert
        Assert.Equal(TestData.TerminalFallSpeed, distances.Max());
    }

    [Fact]
    public void Step_AShortDrop_IsNotWorthHearing()
    {
        // Arrange
        var fall = new BlobFall();
        ScreenCollisionMap map = MapOf(TestData.EmptyScreen());

        // Act - two frames is eight pixels, a fraction of a sprite
        Fall first = fall.Step(map, TestData.BlobAt(0, 0), false, false);
        Fall second = fall.Step(map, TestData.BlobAt(0, first.Distance), false, false);

        // Assert
        Assert.False(first.Audible);
        Assert.False(second.Audible);
    }

    [Fact]
    public void Step_OnceHeHasFallenFurtherThanHeIsTall_TheFallBecomesAudible()
    {
        // Arrange
        var fall = new BlobFall();
        ScreenCollisionMap map = MapOf(TestData.EmptyScreen());

        // Act
        int fallen = 0;
        bool audibleWhileShorterThanASprite = false;
        bool audibleOnce = false;

        for (int frame = 0; frame < 40; frame++)
        {
            Fall step = fall.Step(map, TestData.BlobAt(0, fallen), false, false);
            fallen += step.Distance;

            if (fallen <= SpriteMetrics.SpriteHeight)
            {
                audibleWhileShorterThanASprite |= step.Audible;
            }
            else
            {
                audibleOnce |= step.Audible;
            }
        }

        // Assert
        Assert.False(audibleWhileShorterThanASprite);
        Assert.True(audibleOnce);
    }

    /// <summary>
    ///     A drop through the bottom of one screen onto the next is one fall, so arriving on the new
    ///     screen puts the speed back to the start without forgetting how far he has come. If the total
    ///     were cleared too, the falling sound would cut out at every screen boundary.
    /// </summary>
    [Fact]
    public void StartFallingAgain_PartwayThroughALongDrop_ResetsTheSpeedAndKeepsTheFallAudible()
    {
        // Arrange - fall far enough to be heard
        var fall = new BlobFall();
        ScreenCollisionMap map = MapOf(TestData.EmptyScreen());
        TestData.DropFor(fall, map, 30);

        // Act
        fall.StartFallingAgain();
        Fall afterArriving = fall.Step(map, TestData.BlobAt(0, 0), false, false);

        // Assert
        Assert.Equal(TestData.StartingFallSpeed, afterArriving.Distance);
        Assert.True(afterArriving.Audible);
    }

    [Fact]
    public void Step_LandingAfterADrop_PutsTheFallHeightBackToNothing()
    {
        // Arrange
        var fall = new BlobFall();
        ScreenCollisionMap map = MapOf(TestData.ScreenWithAFloorInRow(1));

        // Act - one frame in the air above the floor, then one with his feet on it
        fall.Step(map, TestData.BlobAt(0, TestData.StandingOnRow1Y - 32), false, false);
        int heightInTheAir = fall.Height;

        fall.Step(map, TestData.BlobAt(0, TestData.StandingOnRow1Y), false, false);

        // Assert
        Assert.True(heightInTheAir > 0);
        Assert.Equal(0, fall.Height);
    }

    /// <summary>
    ///     The shuffle only ever acts on Blob being steered: falling straight down past an opening is
    ///     how he is supposed to miss it.
    /// </summary>
    /// <remarks>
    ///     Only the negative case is covered. Catching the lip needs a fall fast enough to overshoot the
    ///     opening and a ledge placed inside that overshoot, and the probe is a 65-pixel box one blob
    ///     width to the side - so the geometry that triggers it is several tiles of hand-built collision
    ///     cells. It is left uncovered rather than approximated: see <c>GODCLASSES.md</c>.
    /// </remarks>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(64, 100)]
    [InlineData(192, 150)]
    public void Step_NotBeingSteered_FallsTheFullDistanceWhateverIsBesideHim(int xPos, int yPos)
    {
        // Arrange - ledges away to his right and below him, which a steered Blob could be caught by. He
        // is never inside one: these are tile columns 3 and 5 of row 3, and he starts above and left of
        // both.
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[3, 3] = new ScreenTile(TestData.SolidTile);
        screen.ScreenTiles[5, 3] = new ScreenTile(TestData.SolidTile);
        ScreenCollisionMap map = MapOf(screen);

        var fall = new BlobFall();

        // Act
        Fall step = fall.Step(map, TestData.BlobAt(xPos, yPos), false, false);

        // Assert
        Assert.Equal(TestData.StartingFallSpeed, step.Distance);
    }

    private static ScreenCollisionMap MapOf(GameScreenData screen)
    {
        var map = new ScreenCollisionMap([screen], TestData.TileCollisionData());
        map.Rebuild(0);

        return map;
    }

    private static class TestData
    {
        public const int SolidTile = 1;

        /// <summary>The two ends of the gravity curve, as <see cref="BlobFall" /> sets them.</summary>
        public const int StartingFallSpeed = 4;

        /// <inheritdoc cref="StartingFallSpeed" />
        public const int TerminalFallSpeed = 12;

        /// <summary>
        ///     The y at which Blob's feet rest exactly on top of a floor in tile row 1.
        /// </summary>
        public const int StandingOnRow1Y = TileMetrics.TileHeight - SpriteMetrics.SpriteHeight;

        public static GameScreenData EmptyScreen()
            => new(TileMetrics.TilesAcross, TileMetrics.TilesDown);

        public static GameScreenData ScreenWithAFloorInRow(int row)
        {
            GameScreenData screen = EmptyScreen();

            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                screen.ScreenTiles[x, row] = new ScreenTile(SolidTile);
            }

            return screen;
        }

        public static Rectangle BlobAt(int xPos, int yPos)
            => new(xPos, yPos, SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

        /// <summary>
        ///     Drops him for a number of frames, moving him by what each frame allowed - which is what
        ///     <c>Blob.ApplyGravity</c> does with the answer.
        /// </summary>
        public static int[] DropFor(BlobFall fall, ScreenCollisionMap map, int frames)
        {
            int[] distances = new int[frames];
            int yPos = 0;

            for (int frame = 0; frame < frames; frame++)
            {
                distances[frame] = fall.Step(map, BlobAt(0, yPos), false, false).Distance;
                yPos += distances[frame];
            }

            return distances;
        }

        /// <summary>
        ///     Every cell of <see cref="SolidTile" /> solid, and nothing else placed.
        /// </summary>
        public static int[,,] TileCollisionData()
        {
            int[,,] data = new int[
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
