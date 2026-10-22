// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Core;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     A door reading a code off Blob's inventory. The access booths ask for three digits and the
///     Cheops pyramid for two, and both go through the same scan.
/// </summary>
public class AccessCodeLockTests
{
    [Fact]
    public void Scan_WithEveryDigitCarried_OpensTheDoor()
    {
        // Arrange
        Collectables[] inventory = TestData.Carrying(TestData.PickupFor(4), TestData.PickupFor(7), TestData.PickupFor(1));

        // Act
        AccessCodeScan scan = AccessCodeLock.Scan(inventory, [4, 7, 1]);

        // Assert
        Assert.True(scan.Accepted);
    }

    /// <summary>
    ///     Which slot each scanner reads, which is what the status panel highlights while the door
    ///     thinks. The code's order drives it, not the inventory's.
    /// </summary>
    [Fact]
    public void Scan_WithEveryDigitCarried_NamesTheSlotAnsweringEachDigit()
    {
        // Arrange
        Collectables[] inventory = TestData.Carrying(TestData.PickupFor(4), TestData.PickupFor(7), TestData.PickupFor(1));

        // Act
        AccessCodeScan scan = AccessCodeLock.Scan(inventory, [7, 1, 4]);

        // Assert
        Assert.Equal([1, 2, 0], scan.SlotToScan);
    }

    [Fact]
    public void Scan_WithOneDigitMissing_RefusesTheDoor()
    {
        // Arrange
        Collectables[] inventory = TestData.Carrying(TestData.PickupFor(4), TestData.PickupFor(7), Collectables.Key);

        // Act
        AccessCodeScan scan = AccessCodeLock.Scan(inventory, [4, 7, 1]);

        // Assert
        Assert.False(scan.Accepted);
    }

    /// <summary>
    ///     A refused door still says what it did read, because the scanners that found something
    ///     still play their animation before the verdict is announced.
    /// </summary>
    [Fact]
    public void Scan_WithOneDigitMissing_LeavesThatDigitWithNoSlotAndKeepsTheRest()
    {
        // Arrange
        Collectables[] inventory = TestData.Carrying(TestData.PickupFor(4), TestData.PickupFor(7), Collectables.Key);

        // Act
        AccessCodeScan scan = AccessCodeLock.Scan(inventory, [4, 7, 1]);

        // Assert
        Assert.Equal([0, 1, AccessCodeLock.NoSlot], scan.SlotToScan);
    }

    /// <summary>
    ///     What makes the card worth carrying. It answers whatever is asked, so one card opens a
    ///     three-digit door on its own.
    /// </summary>
    [Fact]
    public void Scan_CarryingOnlyTheAccessCard_OpensADoorOfAnyLength()
    {
        // Arrange
        Collectables[] inventory = TestData.Carrying(Collectables.AccessCard);

        // Act
        AccessCodeScan scan = AccessCodeLock.Scan(inventory, [4, 7, 1]);

        // Assert
        Assert.True(scan.Accepted);
    }

    [Fact]
    public void Scan_CarryingOnlyTheAccessCard_SendsEveryScannerToTheCardsSlot()
    {
        // Arrange
        Collectables[] inventory = TestData.Carrying(Collectables.Key, Collectables.AccessCard);

        // Act
        AccessCodeScan scan = AccessCodeLock.Scan(inventory, [4, 7]);

        // Assert
        Assert.Equal([1, 1], scan.SlotToScan);
    }

    /// <summary>
    ///     The key opens key doors, not booth doors. It sits next to the card in
    ///     <see cref="Collectables" /> and is the obvious thing for the scan to have confused with
    ///     it.
    /// </summary>
    [Fact]
    public void Scan_CarryingOnlyTheKey_RefusesTheDoor()
    {
        // Arrange
        Collectables[] inventory = TestData.Carrying(Collectables.Key);

        // Act
        AccessCodeScan scan = AccessCodeLock.Scan(inventory, [4]);

        // Assert
        Assert.False(scan.Accepted);
    }

    [Fact]
    public void Scan_WithAnEmptyInventory_RefusesTheDoor()
    {
        // Arrange
        Collectables[] inventory = TestData.Carrying(Collectables.None, Collectables.None);

        // Act
        AccessCodeScan scan = AccessCodeLock.Scan(inventory, [0]);

        // Assert
        Assert.False(scan.Accepted);
    }

    /// <summary>
    ///     An empty slot reads as <see cref="Collectables.None" />, which is 255 - so the digit it
    ///     would answer is 245, outside anything a code can ask for. The arithmetic is what keeps an
    ///     empty slot from matching rather than a check, and this pins that it does.
    /// </summary>
    [Fact]
    public void Scan_WithTheDigitBehindAnEmptySlot_ReadsPastTheEmptySlot()
    {
        // Arrange
        Collectables[] inventory = TestData.Carrying(Collectables.None, TestData.PickupFor(3));

        // Act
        AccessCodeScan scan = AccessCodeLock.Scan(inventory, [3]);

        // Assert
        Assert.Equal([1], scan.SlotToScan);
    }

    /// <summary>
    ///     Two slots answering the same digit is ordinary - the world generator only stops a code
    ///     repeating a digit, not Blob carrying a pick-up twice.
    /// </summary>
    [Fact]
    public void Scan_WithTwoSlotsAnsweringTheSameDigit_TakesTheFirst()
    {
        // Arrange
        Collectables[] inventory = TestData.Carrying(TestData.PickupFor(6), TestData.PickupFor(6));

        // Act
        AccessCodeScan scan = AccessCodeLock.Scan(inventory, [6]);

        // Assert
        Assert.Equal([0], scan.SlotToScan);
    }

    [Fact]
    public void Digits_FromACode_ReadsOneDigitPerCharacter()
    {
        // Act
        int[] digits = AccessCodeLock.Digits("471", 3);

        // Assert
        Assert.Equal([4, 7, 1], digits);
    }

    [Fact]
    public void Digits_FromAPyramidsTwoDigitCode_ReadsBoth()
    {
        // Act
        int[] digits = AccessCodeLock.Digits("80", 2);

        // Assert
        Assert.Equal([8, 0], digits);
    }

    /// <summary>
    ///     A screen with no door on it. Every digit reads as no slot, which nothing in the inventory
    ///     can answer - so a door with no code never opens, rather than opening for anyone.
    /// </summary>
    [Fact]
    public void Digits_FromNoCodeAtAll_AreAllUnreadable()
    {
        // Act
        int[] digits = AccessCodeLock.Digits(null, 3);

        // Assert
        Assert.Equal([AccessCodeLock.NoSlot, AccessCodeLock.NoSlot, AccessCodeLock.NoSlot], digits);
    }

    [Fact]
    public void Scan_AgainstAnUnreadCode_RefusesTheDoor()
    {
        // Arrange
        Collectables[] inventory = TestData.Carrying(TestData.PickupFor(0), TestData.PickupFor(1));
        int[] unread = AccessCodeLock.Digits(null, 2);

        // Act
        AccessCodeScan scan = AccessCodeLock.Scan(inventory, unread);

        // Assert
        Assert.False(scan.Accepted);
    }

    private static class TestData
    {
        /// <summary>
        ///     The pick-up that answers <paramref name="digit" />. Ten of the twenty-eight core
        ///     element types read as digits, starting at
        ///     <see cref="AccessCodeLock.FirstDigitPickup" />.
        /// </summary>
        public static Collectables PickupFor(int digit)
            => (Collectables)(AccessCodeLock.FirstDigitPickup + digit);

        public static Collectables[] Carrying(params Collectables[] items) => items;
    }
}
