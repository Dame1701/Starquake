// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using MessagePack;
using Starquake.Constants;
using Starquake.SharedLibs;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     Where the planet core is, now that the map says rather than the game.
/// </summary>
/// <remarks>
///     The core is the one authored position that is not a <see cref="MarkerTile" />: it covers one and
///     a half tiles by two and has never been tile-aligned, so it is stored as a position and placed to
///     <see cref="CoreSite.PositionStep" /> pixels. That makes the rounding and the clamping worth
///     pinning - a core placed part way off the screen would draw elements nobody can reach.
/// </remarks>
public class CoreSiteTests
{

    /// <summary>
    ///     The area the editor outlines has to be the area the game fills, or the author is placing the
    ///     core against a box that lies. <c>CoreElements</c> takes its grid from <see cref="CoreSite" />,
    ///     so only the sprite size is said in two places - and this is what holds the two together.
    /// </summary>
    [Fact]
    public void SpriteSize_IsTheGamesOwnSpriteSize()
    {
        Assert.Equal(SpriteMetrics.SpriteWidth, CoreSite.SpriteSize);
        Assert.Equal(SpriteMetrics.SpriteHeight, CoreSite.SpriteSize);
    }

    [Fact]
    public void Area_IsNineElementsThreeByThree()
    {
        Assert.Equal(192, CoreSite.AreaWidth);
        Assert.Equal(192, CoreSite.AreaHeight);
        Assert.Equal(9, CoreSite.SpritesAcross * CoreSite.SpritesDown);
    }

    /// <summary>
    ///     The reason the core cannot be a marker tile: the position the game shipped with does not sit
    ///     on a tile boundary, so the tile grid could not express it.
    /// </summary>
    [Fact]
    public void Original_IsNotOnATileBoundary()
        => Assert.NotEqual(0, CoreSite.Original.XPos % TileMetrics.TileWidth);

    /// <summary>
    ///     And the reason the step is sixteen: it is fine enough to express where the core already is.
    /// </summary>
    [Fact]
    public void Original_IsOnThePositionStep()
    {
        Assert.Equal(0, CoreSite.Original.XPos % CoreSite.PositionStep);
        Assert.Equal(0, CoreSite.Original.YPos % CoreSite.PositionStep);
    }

    [Fact]
    public void FindIn_APlacedCore_IsReadOffTheMap()
    {
        // Arrange
        GameScreenData[] world = TestData.World();
        world[317].Core = new CorePlacement(128, 96);

        // Act and assert
        Assert.Equal(new CoreSite(317, 128, 96), CoreSite.FindIn(world));
    }

    /// <summary>
    ///     A map saved before the position was stored still has to have a core, or the game cannot be
    ///     finished at all.
    /// </summary>
    [Fact]
    public void FindIn_NoCoreOnTheMap_FallsBackToWhereTheGameKeptIt()
        => Assert.Equal(CoreSite.Original, CoreSite.FindIn(TestData.World()));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(7, 0)]
    [InlineData(8, 16)]
    [InlineData(23, 16)]
    [InlineData(24, 32)]
    [InlineData(416, 416)]
    public void Snap_APosition_IsRoundedToTheStep(int given, int expected)
        => Assert.Equal(expected, CoreSite.Snap(given, 0).XPos);

    /// <summary>
    ///     Held inside the screen, so the whole core stays on it. Dragging towards a corner should stop
    ///     against it rather than push elements off the edge.
    /// </summary>
    [Fact]
    public void Snap_APositionPastTheEdge_KeepsTheWholeCoreOnTheScreen()
    {
        // Act
        CorePlacement far = CoreSite.Snap(10_000, 10_000);
        CorePlacement near = CoreSite.Snap(-10_000, -10_000);

        // Assert
        Assert.Equal(TileMetrics.PlayAreaWidth - CoreSite.AreaWidth, far.XPos);
        Assert.Equal(TileMetrics.PlayAreaHeight - CoreSite.AreaHeight, far.YPos);
        Assert.Equal(0, near.XPos);
        Assert.Equal(0, near.YPos);
    }

    /// <summary>
    ///     The rule the editor leans on: placing the core somewhere takes it off wherever it was. A
    ///     planet has one core, and <see cref="CoreSite.FindIn" /> takes the first screen carrying one,
    ///     so a second would not be refused - it would simply never be found.
    /// </summary>
    [Fact]
    public void PlaceOn_ASecondScreen_TakesTheCoreOffTheFirst()
    {
        // Arrange
        GameScreenData[] world = TestData.World();
        CoreSite.PlaceOn(world, 199, new CorePlacement(416, 192));

        // Act
        CoreSite.PlaceOn(world, 42, new CorePlacement(0, 0));

        // Assert
        Assert.Null(world[199].Core);
        Assert.Equal(1, CoreSite.CountOn(world));
        Assert.Equal(new CoreSite(42, 0, 0), CoreSite.FindIn(world));
    }

    /// <summary>
    ///     However many times it is moved, which is what dragging it about amounts to.
    /// </summary>
    [Fact]
    public void PlaceOn_MovedRepeatedly_LeavesExactlyOneCore()
    {
        // Arrange
        GameScreenData[] world = TestData.World();

        // Act
        foreach (int screen in new[]
                 {
                     0,
                     5,
                     199,
                     42,
                     511,
                     199,
                     8
                 })
        {
            CoreSite.PlaceOn(world, screen, new CorePlacement(64, 64));
        }

        // Assert
        Assert.Equal(1, CoreSite.CountOn(world));
        Assert.Equal(8, CoreSite.FindIn(world).ScreenNumber);
    }

    /// <summary>
    ///     Moving it within one screen is a move, not a second core.
    /// </summary>
    [Fact]
    public void PlaceOn_TheSameScreenTwice_LeavesOneCoreAtTheNewPosition()
    {
        // Arrange
        GameScreenData[] world = TestData.World();
        CoreSite.PlaceOn(world, 199, new CorePlacement(416, 192));

        // Act
        CoreSite.PlaceOn(world, 199, new CorePlacement(128, 96));

        // Assert
        Assert.Equal(1, CoreSite.CountOn(world));
        Assert.Equal(new CoreSite(199, 128, 96), CoreSite.FindIn(world));
    }

    [Fact]
    public void PlaceOn_NothingToChange_SaysSo()
    {
        // Arrange
        GameScreenData[] world = TestData.World();
        CoreSite.PlaceOn(world, 199, new CorePlacement(416, 192));

        // Act and assert - the same core in the same place is not a move
        Assert.False(CoreSite.PlaceOn(world, 199, new CorePlacement(416, 192)));
        Assert.True(CoreSite.PlaceOn(world, 199, new CorePlacement(416, 208)));
    }

    /// <summary>
    ///     Taking the core off the map entirely, which is what an undo back past the first placement
    ///     does. The game then falls back to where it always kept it.
    /// </summary>
    [Fact]
    public void PlaceOn_Nothing_LeavesTheMapWithNoCore()
    {
        // Arrange
        GameScreenData[] world = TestData.World();
        CoreSite.PlaceOn(world, 42, new CorePlacement(64, 64));

        // Act
        CoreSite.PlaceOn(world, 42, null);

        // Assert
        Assert.Equal(0, CoreSite.CountOn(world));
        Assert.Equal(CoreSite.Original, CoreSite.FindIn(world));
    }

    [Fact]
    public void CountOn_TheShippedPlanet_HasExactlyOneCore()
    {
        // Arrange
        (GameScreenData[] world, _) = ShippedMap.Load();

        // Act and assert
        Assert.Equal(1, CoreSite.CountOn(world));
    }

    [Fact]
    public void Core_SerialisedWithAScreen_SurvivesTheRoundTrip()
    {
        // Arrange
        var screen = new GameScreenData(1, 1)
        {
            Core = new CorePlacement(416, 192)
        };

        // Act
        GameScreenData restored = MessagePackSerializer.Deserialize<GameScreenData>(
            MessagePackSerializer.Serialize(screen));

        // Assert
        Assert.Equal(new CorePlacement(416, 192), restored.Core);
    }

    [Fact]
    public void Core_AScreenWithoutOne_RoundTripsAsNothing()
    {
        // Arrange
        var screen = new GameScreenData(1, 1);

        // Act
        GameScreenData restored = MessagePackSerializer.Deserialize<GameScreenData>(
            MessagePackSerializer.Serialize(screen));

        // Assert
        Assert.Null(restored.Core);
    }

    /// <summary>
    ///     The shipped planet now carries its core rather than relying on the fallback, and it is where
    ///     the game always had it.
    /// </summary>
    [Fact]
    public void FindIn_TheShippedPlanet_PlacesTheCoreWhereItAlwaysWas()
    {
        // Arrange
        (GameScreenData[] world, _) = ShippedMap.Load();

        // Act and assert
        Assert.Contains(world, screen => screen.Core is not null);
        Assert.Equal(CoreSite.Original, CoreSite.FindIn(world));
    }

    private static class TestData
    {
        /// <summary>
        ///     A planet of blank screens. These tests are about which screen carries the core, so
        ///     anything else painted on one is noise.
        /// </summary>
        public static GameScreenData[] World()
        {
            var world = new GameScreenData[WorldMap.ScreenCount];

            for (int screen = 0; screen < world.Length; screen++)
            {
                world[screen] = new GameScreenData(TileMetrics.TilesAcross, TileMetrics.TilesDown);
            }

            return world;
        }
    }
}
