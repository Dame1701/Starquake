// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Starquake.ActiveSprites.BlobParts;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.SharedLibs;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The two parts of <c>Blob</c> that are about arithmetic rather than about drawing: getting him
///     out of scenery he has ended up inside, and the four things he can carry.
/// </summary>
public class UnstickProbeTests
{
    [Fact]
    public void FreePositionFor_StandingInClearAir_LeavesHimWhereHeIs()
    {
        // Arrange
        UnstickProbe probe = ProbeOn(TestData.EmptyScreen());
        var blob = new Rectangle(TestData.Middle, TestData.Middle, TestData.BlobSize, TestData.BlobSize);

        // Act
        Point freed = probe.FreePositionFor(blob);

        // Assert
        Assert.Equal(blob.Location, freed);
    }

    [Fact]
    public void FreePositionFor_HisFeetBuriedInATile_PushesHimUpOutOfIt()
    {
        // Arrange - a solid tile filling row 2, with the blob's feet a few pixels into it
        GameScreenData screen = TestData.EmptyScreen();
        TestData.FillRow(screen, 2);

        UnstickProbe probe = ProbeOn(screen);

        int buriedBy = 4;
        var blob = new Rectangle(
            0,
            2 * TileMetrics.TileHeight - TestData.BlobSize + buriedBy,
            TestData.BlobSize,
            TestData.BlobSize);

        // Act
        Point freed = probe.FreePositionFor(blob);

        // Assert
        Assert.Equal(blob.X, freed.X);
        Assert.True(freed.Y < blob.Y, "He should have been pushed up out of the floor");
    }

    [Fact]
    public void FreePositionFor_HisHeadBuriedInATile_PushesHimDownOutOfIt()
    {
        // Arrange - a ceiling in row 1, with the blob's head a few pixels into it
        GameScreenData screen = TestData.EmptyScreen();
        TestData.FillRow(screen, 1);

        UnstickProbe probe = ProbeOn(screen);

        int buriedBy = 4;
        var blob = new Rectangle(
            0,
            2 * TileMetrics.TileHeight - buriedBy,
            TestData.BlobSize,
            TestData.BlobSize);

        // Act
        Point freed = probe.FreePositionFor(blob);

        // Assert
        Assert.True(freed.Y > blob.Y, "He should have been pushed down out of the ceiling");
    }

    /// <summary>
    ///     Caught on both sides means there is nowhere to push him, so he is left where he is rather
    ///     than being walked through the scenery looking for room.
    /// </summary>
    [Fact]
    public void FreePositionFor_WalledInOnBothSides_LeavesHimWhereHeIs()
    {
        // Arrange - every tile solid
        GameScreenData screen = TestData.EmptyScreen();

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            TestData.FillRow(screen, y);
        }

        UnstickProbe probe = ProbeOn(screen);
        var blob = new Rectangle(TestData.Middle, TestData.Middle, TestData.BlobSize, TestData.BlobSize);

        // Act
        Point freed = probe.FreePositionFor(blob);

        // Assert
        Assert.Equal(blob.Location, freed);
    }

    private static UnstickProbe ProbeOn(GameScreenData screen)
    {
        var map = new ScreenCollisionMap([screen], TestData.TileCollisionData());
        map.Rebuild(0);

        return new UnstickProbe(map);
    }

    internal static class TestData
    {
        public const int SolidTile = 1;

        public const int BlobSize = 32;

        public const int Middle = 300;

        public static GameScreenData EmptyScreen()
            => new(TileMetrics.TilesAcross, TileMetrics.TilesDown);

        public static void FillRow(GameScreenData screen, int row)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                screen.ScreenTiles[x, row] = new ScreenTile(SolidTile);
            }
        }

        public static int[,,] TileCollisionData()
        {
            var data = new int[
                512,
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

/// <summary>
///     The four things the blob carries. A queue rather than a set of slots, which is what makes
///     the game's puzzles what they are: to get at something three places back he has to cycle
///     everything in front of it out onto the floor.
/// </summary>
public class InventoryTests
{
    [Fact]
    public void NewInventory_IsEmpty()
    {
        // Arrange and act
        var inventory = new Inventory();

        // Assert
        Assert.Equal(Inventory.Capacity, inventory.Items.Length);
        Assert.All(inventory.Items, item => Assert.Equal(Collectables.None, item));
    }

    [Fact]
    public void SwapWithFloor_StandingOnAnItemWithNothingHeld_PicksItUpIntoTheFrontSlot()
    {
        // Arrange
        var inventory = new Inventory();
        Rectangle blob = TestData.BlobAt(0, 0);
        List<InteractiveItem> floor = [TestData.ItemUnder(blob, (int)Collectables.Key)];

        // Act
        inventory.SwapWithFloor(blob, floor);

        // Assert
        Assert.Equal(Collectables.Key, inventory.Items[0]);
        Assert.Empty(floor);
    }

    [Fact]
    public void SwapWithFloor_PickingUpASecondItem_PushesTheFirstOneBack()
    {
        // Arrange
        var inventory = new Inventory();
        Rectangle blob = TestData.BlobAt(0, 0);

        List<InteractiveItem> floor = [TestData.ItemUnder(blob, (int)Collectables.Key)];
        inventory.SwapWithFloor(blob, floor);

        floor.Add(TestData.ItemUnder(blob, (int)Collectables.AccessCard));

        // Act
        inventory.SwapWithFloor(blob, floor);

        // Assert
        Assert.Equal(Collectables.AccessCard, inventory.Items[0]);
        Assert.Equal(Collectables.Key, inventory.Items[1]);
    }

    [Fact]
    public void SwapWithFloor_Full_DropsTheBackOfTheQueueOnTheFloor()
    {
        // Arrange - carry four things
        var inventory = new Inventory();
        Rectangle blob = TestData.BlobAt(0, 0);

        for (int item = 0; item < Inventory.Capacity; item++)
        {
            inventory.SwapWithFloor(blob, [TestData.ItemUnder(blob, item)]);
        }

        Collectables lastIn = inventory.Items[^1];
        var floor = new List<InteractiveItem>();

        // Act - nothing underfoot, so this is a pure drop
        inventory.SwapWithFloor(blob, floor);

        // Assert
        Assert.Single(floor);
        Assert.Equal((int)lastIn, floor[0].Type);
        Assert.Equal(Collectables.None, inventory.Items[0]);
    }

    /// <summary>
    ///     The dropped item lands under the blob's feet, so without this the swap would hand it
    ///     straight back and nothing would ever cycle.
    /// </summary>
    [Fact]
    public void SwapWithFloor_TheItemJustDropped_IsNotPickedStraightBackUp()
    {
        // Arrange
        var inventory = new Inventory();
        Rectangle blob = TestData.BlobAt(0, 0);

        var floor = new List<InteractiveItem>();
        inventory.SwapWithFloor(blob, [TestData.ItemUnder(blob, (int)Collectables.Key)]);

        for (int item = 0; item < Inventory.Capacity - 1; item++)
        {
            inventory.SwapWithFloor(blob, [TestData.ItemUnder(blob, item)]);
        }

        // Act - the key is at the back and is dropped; it lands underfoot
        inventory.SwapWithFloor(blob, floor);

        // Assert
        Assert.Single(floor);
        Assert.Equal((int)Collectables.Key, floor[0].Type);
        Assert.DoesNotContain(Collectables.Key, inventory.Items);
    }

    [Fact]
    public void SwapWithFloor_ADroppedItem_IsSnappedToTheCollisionGrid()
    {
        // Arrange
        var inventory = new Inventory();
        Rectangle blob = TestData.BlobAt(TileMetrics.CollisionCellWidth + 5, TileMetrics.CollisionCellHeight + 7);

        inventory.SwapWithFloor(blob, [TestData.ItemUnder(blob, (int)Collectables.Key)]);

        for (int item = 0; item < Inventory.Capacity - 1; item++)
        {
            inventory.SwapWithFloor(blob, [TestData.ItemUnder(blob, item)]);
        }

        var floor = new List<InteractiveItem>();

        // Act
        inventory.SwapWithFloor(blob, floor);

        // Assert
        Assert.Equal(0, floor[0].XLocation % TileMetrics.CollisionCellWidth);
        Assert.Equal(0, floor[0].YLocation % TileMetrics.CollisionCellHeight);
    }

    [Fact]
    public void SwapWithFloor_NothingHeldAndNothingUnderfoot_ChangesNothing()
    {
        // Arrange
        var inventory = new Inventory();
        var floor = new List<InteractiveItem>();

        // Act
        inventory.SwapWithFloor(TestData.BlobAt(0, 0), floor);

        // Assert
        Assert.Empty(floor);
        Assert.All(inventory.Items, item => Assert.Equal(Collectables.None, item));
    }

    private static class TestData
    {
        public static Rectangle BlobAt(int x, int y)
            => new(x, y, UnstickProbeTests.TestData.BlobSize, UnstickProbeTests.TestData.BlobSize);

        /// <summary>
        ///     An item placed where the blob standing at <paramref name="blob" /> is touching it.
        ///     The pick-up test offsets the item's box, so this undoes that offset.
        /// </summary>
        public static InteractiveItem ItemUnder(Rectangle blob, int type)
            => new(
                blob.X - SpriteMetrics.SpriteWidth / 4,
                blob.Y - TileMetrics.CollisionCellHeight,
                type);
    }
}
