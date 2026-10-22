// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Core;
using Starquake.Engines;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     F4's two rules - which slot a handed-over core element goes into, and which element it is -
///     and F7's one, which is that it hands over nothing Blob already has. The keys themselves need
///     a keyboard and a running game. These are the parts that do not.
/// </summary>
public class TestModeKeysTests
{
    [Fact]
    public void FirstEmptySlot_WithRoomToSpare_IsTheFirstOneHoldingNothing()
    {
        // Arrange
        Collectables[] carried =
            [Collectables.None, Collectables.None, Collectables.None, Collectables.None];

        carried[0] = Collectables.Key;

        // Act / Assert
        Assert.Equal(1, TestModeKeys.FirstEmptySlot(carried));
    }

    /// <summary>
    ///     A full inventory has to answer null rather than zero, or the key would overwrite whatever
    ///     Blob was carrying in his first slot.
    /// </summary>
    [Fact]
    public void FirstEmptySlot_WhenHeIsFull_IsNothingAtAll()
    {
        // Arrange
        Collectables[] carried =
            [Collectables.Key, Collectables.AccessCard, Collectables.Key, Collectables.AccessCard];

        // Act / Assert
        Assert.Null(TestModeKeys.FirstEmptySlot(carried));
    }

    [Fact]
    public void FirstEmptySlot_WithAGapInTheMiddle_FindsIt()
    {
        // Arrange
        Collectables[] carried =
            [Collectables.Key, Collectables.None, Collectables.AccessCard, Collectables.Key];

        // Act / Assert
        Assert.Equal(1, TestModeKeys.FirstEmptySlot(carried));
    }

    /// <summary>
    ///     F7 hands over the key and the access card, and may be pressed again to top Blob up after
    ///     a booth has taken his card. Pressing it while he already has one must not spend a second
    ///     slot on a duplicate. He has four slots, and the core wants all of them.
    /// </summary>
    [Fact]
    public void SlotFor_SomethingHeIsAlreadyCarrying_IsNothingAtAll()
    {
        // Arrange
        Collectables[] carried =
            [Collectables.Key, Collectables.None, Collectables.None, Collectables.None];

        // Act / Assert
        Assert.Null(TestModeKeys.SlotFor(carried, Collectables.Key));
    }

    [Fact]
    public void SlotFor_SomethingHeIsNotCarrying_IsTheFirstEmptySlot()
    {
        // Arrange
        Collectables[] carried =
            [Collectables.Key, Collectables.None, Collectables.None, Collectables.None];

        // Act / Assert
        Assert.Equal(1, TestModeKeys.SlotFor(carried, Collectables.AccessCard));
    }

    [Fact]
    public void SlotFor_WhenHeIsFull_IsNothingAtAll()
    {
        // Arrange
        Collectables[] carried =
            [(Collectables)3, (Collectables)7, (Collectables)11, (Collectables)14];

        // Act / Assert
        Assert.Null(TestModeKeys.SlotFor(carried, Collectables.Key));
    }

    /// <summary>
    ///     The core matches every inventory slot against every element it wants, so two copies of
    ///     one element would queue it twice - scoring it twice and starting its replacement again
    ///     while the first was still running. Pressing the key four times must therefore hand over
    ///     four different things.
    /// </summary>
    [Fact]
    public void WantedElementNotYetCarried_WhenHeAlreadyHasOne_PicksAnother()
    {
        // Arrange - the nine the core is missing, as their element numbers
        int[] wanted = [3, 7, 11, 14, 19, 22, 24, 25, 27];

        Collectables[] carried =
            [Collectables.None, Collectables.None, Collectables.None, Collectables.None];

        // Act - take four in a row, putting each into his hands as the key would
        List<int> handedOver = [];

        for (int press = 0; press < 4; press++)
        {
            int? element = TestModeKeys.WantedElementNotYetCarried(wanted, carried);

            Assert.NotNull(element);

            handedOver.Add(element.Value);
            carried[press] = (Collectables)element.Value;
        }

        // Assert
        Assert.Equal(4, handedOver.Distinct().Count());
    }

    /// <summary>
    ///     Once he is carrying all there is left to want, the key has nothing to give and must say
    ///     so rather than handing over something the core has already had.
    /// </summary>
    [Fact]
    public void WantedElementNotYetCarried_WhenHeHasThemAll_IsNothingAtAll()
    {
        // Arrange
        int[] wanted = [3, 7];

        Collectables[] carried =
            [(Collectables)3, (Collectables)7, Collectables.None, Collectables.None];

        // Act / Assert
        Assert.Null(TestModeKeys.WantedElementNotYetCarried(wanted, carried));
    }

    [Fact]
    public void WantedElementNotYetCarried_WhenTheCoreWantsNothing_IsNothingAtAll()
        => Assert.Null(TestModeKeys.WantedElementNotYetCarried(
            [],
            [Collectables.None, Collectables.None, Collectables.None, Collectables.None]));
}
