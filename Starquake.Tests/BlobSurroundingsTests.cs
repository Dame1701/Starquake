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
///     The four readings Blob decides almost everything about his surroundings from. These were
///     four fields on <c>Blob</c> written by one method and read by eight, so none of it could be
///     reached without Blob, and Blob needs the content pipeline and a graphics device.
/// </summary>
/// <remarks>
///     The probe reports which tile <em>image</em> is in the way, not whether it is solid - the
///     collision mask is never consulted. That is deliberate and is what the callers need: they switch
///     on the tile number to find a shuttle pad or a tunnel mouth, both of which are walked into rather
///     than blocked by. So these tests place tiles and never set a collision cell.
/// </remarks>
public class BlobSurroundingsTests
{
    [Fact]
    public void Around_AnEmptyScreen_FindsNothingInAnyDirection()
    {
        // Arrange
        ScreenCollisionMap map = MapOf(TestData.EmptyScreen());

        // Act
        BlobSurroundings surroundings = TestData.Probe(map, TestData.MoverX, TestData.MoverY);

        // Assert
        Assert.Null(surroundings.Above);
        Assert.Null(surroundings.Below);
        Assert.Null(surroundings.Left);
        Assert.Null(surroundings.Right);
    }

    [Theory]
    [InlineData(1, 1, "Left")]
    [InlineData(3, 1, "Right")]
    [InlineData(2, 0, "Above")]
    [InlineData(2, 2, "Below")]
    public void Around_OneTileOnEachSide_ReportsItAgainstThatSideAndNoOther(int tileX, int tileY, string side)
    {
        // Arrange. The mover sits in tile (2, 1) with a gap all round it, so each neighbour is reachable
        // in exactly one direction.
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[tileX, tileY] = new ScreenTile(TestData.AnyTile);
        ScreenCollisionMap map = MapOf(screen);

        // Act
        BlobSurroundings surroundings = TestData.Probe(map, TestData.MoverX, TestData.MoverY);

        // Assert
        Assert.Equal(side == "Above" ? tileX : null, surroundings.Above?.MapXPosition);
        Assert.Equal(side == "Below" ? tileX : null, surroundings.Below?.MapXPosition);
        Assert.Equal(side == "Left" ? tileX : null, surroundings.Left?.MapXPosition);
        Assert.Equal(side == "Right" ? tileX : null, surroundings.Right?.MapXPosition);
    }

    /// <summary>
    ///     The reason <c>Around</c> takes a separate height to probe downwards with. Blob riding the
    ///     shuttle is a shuttle's height taller, and that height is underneath him, so the ground has to
    ///     be looked for from his own feet - otherwise he reads the floor through the shuttle.
    /// </summary>
    [Fact]
    public void Around_AShorterReachBelow_StopsBeforeTheFloorTheFullHeightWouldFind()
    {
        // Arrange. A floor along the row under the mover, which stands one tile tall at the top of its
        // own tile, so its feet are exactly on the boundary.
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[0, 1] = new ScreenTile(TestData.AnyTile);
        ScreenCollisionMap map = MapOf(screen);

        // Act. A short reach, so the two heights land in different tile rows rather than both
        // spanning the boundary.
        TileDetected? fullHeight = BlobSurroundings.Around(
            map, 0, 0, TestData.MoverSize, TileMetrics.TileHeight,
            TileMetrics.TileHeight, TestData.ShortReach, TestData.ShortReach).Below;

        TileDetected? shortenedByAShuttle = BlobSurroundings.Around(
            map, 0, 0, TestData.MoverSize, TileMetrics.TileHeight,
            TileMetrics.TileHeight - TestData.ShuttleHeight, TestData.ShortReach, TestData.ShortReach).Below;

        // Assert
        Assert.Equal(1, fullHeight?.MapYPosition);
        Assert.Null(shortenedByAShuttle);
    }

    [Fact]
    public void Around_AShorterReachBelow_LeavesTheOtherThreeDirectionsAlone()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[1, 1] = new ScreenTile(TestData.AnyTile);
        ScreenCollisionMap map = MapOf(screen);

        // Act
        BlobSurroundings shortened = BlobSurroundings.Around(
            map, TestData.MoverX, TestData.MoverY, TestData.MoverSize, TestData.MoverSize,
            TestData.MoverSize - TestData.ShuttleHeight, TestData.Reach, TestData.Reach);

        // Assert
        Assert.Equal(1, shortened.Left?.MapXPosition);
        Assert.Null(shortened.Above);
        Assert.Null(shortened.Right);
    }

    [Fact]
    public void Around_TheTileFound_CarriesWhereInsideItTheMoverMetIt()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[1, 1] = new ScreenTile(TestData.AnyTile);
        ScreenCollisionMap map = MapOf(screen);

        // Act
        TileDetected? toTheLeft = TestData.Probe(map, TestData.MoverX, TestData.MoverY).Left;

        // Assert. A horizontal check reports where the mover's top sits inside the tile, measured from
        // that tile's origin - which is what the booth and tunnel-mouth tests in Blob compare against.
        Assert.Equal(TestData.AnyTile, toTheLeft?.TileNumber);
        Assert.Equal(TestData.MoverX - TileMetrics.TileWidth, toTheLeft?.XPosition);
        Assert.Equal(TestData.MoverY - TileMetrics.TileHeight, toTheLeft?.YPosition);
    }

    private static ScreenCollisionMap MapOf(GameScreenData screen)
        => new([screen], TestData.TileCollisionData());

    private static class TestData
    {
        /// <summary>
        ///     Any tile image at all. Which one does not matter here: the probe reports presence and
        ///     leaves the meaning of the number to the caller.
        /// </summary>
        public const int AnyTile = 1;

        /// <summary>A square mover, so the same reach in every direction means the same clearance.</summary>
        public const int MoverSize = 32;

        /// <summary>
        ///     How far each probe looks. A probe reaches exactly this far past the mover, so reaching a
        ///     neighbouring tile from the middle of your own takes a reach of half a tile - the real
        ///     Blob is nearly a tile wide and needs only his walking speed.
        /// </summary>
        public const int Reach = 64;

        /// <inheritdoc cref="Reach" />
        public const int ShortReach = 8;

        /// <summary>How much taller Blob is while riding the shuttle.</summary>
        public const int ShuttleHeight = 32;

        /// <summary>
        ///     Wholly inside tile (2, 1) and <see cref="Reach" /> from the boundary on all four sides, so
        ///     each of the four probes lands in exactly one neighbouring tile and none of them reports
        ///     the tile the mover is standing in.
        /// </summary>
        public const int MoverX = 2 * TileMetrics.TileWidth + 48;

        /// <inheritdoc cref="MoverX" />
        public const int MoverY = 1 * TileMetrics.TileHeight + 32;

        public static GameScreenData EmptyScreen()
            => new(TileMetrics.TilesAcross, TileMetrics.TilesDown);

        /// <summary>
        ///     A square mover reaching the same distance in every direction, so a test that places one
        ///     tile is asserting about direction and nothing else.
        /// </summary>
        public static BlobSurroundings Probe(ScreenCollisionMap map, int xPos, int yPos)
            => BlobSurroundings.Around(
                map, xPos, yPos, MoverSize, MoverSize, MoverSize, Reach, Reach);

        /// <summary>
        ///     The map needs collision data to construct, and none of these tests reads it - the probe
        ///     consults tile images, not cells.
        /// </summary>
        public static int[,,] TileCollisionData()
            => new int[
                AnyTile + 1,
                TileMetrics.CollisionCellsAcrossTile,
                TileMetrics.CollisionCellsDownTile];
    }
}
