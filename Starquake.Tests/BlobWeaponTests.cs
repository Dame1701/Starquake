// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.ActiveSprites;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The two decisions taken as Blob's shot is launched: which way it leaves him, and how far it
///     has to get before the thing he is standing on counts. <see cref="BlobFire" /> and
///     <see cref="IFireSurroundings" /> cover where it goes once it is flying.
/// </summary>
/// <remarks>
///     Only the rules that are pure arithmetic are covered, and that is the honest limit of the
///     extraction: the rest of <see cref="BlobWeapon" /> pulls two particle effects and a sprite
///     sheet out of the resource manager before it exists, so the class cannot be constructed
///     without the content pipeline. The plan for this stage claimed otherwise - see
///     <c>GODCLASSES.md</c>.
/// </remarks>
public class BlobWeaponTests
{
    // The theories below name Direction, which is internal to Starquake.Shared and reaches here only
    // through InternalsVisibleTo - not enough to let a public signature name one. xunit requires the
    // test class to be public, not its methods, so dropping those to internal costs nothing. Same
    // reasoning as BlobFireTests.
    [Theory]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Down)]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Right)]
    internal void DirectionOfShot_OnTheShuttle_FiresWhicheverWayHeIsMoving(Direction moving)
    {
        // Act
        Direction fired = BlobWeapon.DirectionOfShot(moving, true, Direction.Left);

        // Assert
        Assert.Equal(moving, fired);
    }

    [Theory]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Right)]
    internal void DirectionOfShot_OnFootMovingSideways_FiresThatWay(Direction moving)
    {
        // Act
        Direction fired = BlobWeapon.DirectionOfShot(moving, false, Direction.Up);

        // Assert
        Assert.Equal(moving, fired);
    }

    /// <summary>
    ///     The reason the last direction is carried rather than recomputed: Blob standing still has no
    ///     direction of travel, and he has to fire somewhere.
    /// </summary>
    [Theory]
    [InlineData(Direction.None)]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Down)]
    internal void DirectionOfShot_OnFootNotMovingSideways_KeepsTheWayHeLastFired(Direction moving)
    {
        // Act
        Direction fired = BlobWeapon.DirectionOfShot(moving, false, Direction.Right);

        // Assert
        Assert.Equal(Direction.Right, fired);
    }

    /// <summary>
    ///     Worth pinning because it looks like an oversight and is not reachable in play: the shuttle arm
    ///     takes the direction of travel whatever it is, and Blob on a shuttle that is not moving has
    ///     already been given a direction by the frame he got on.
    /// </summary>
    [Fact]
    public void DirectionOfShot_OnTheShuttleAndNotMoving_TakesTheDirectionOfTravelAnyway()
    {
        // Act
        Direction fired = BlobWeapon.DirectionOfShot(Direction.None, true, Direction.Right);

        // Assert
        Assert.Equal(Direction.None, fired);
    }

    [Fact]
    public void DirectionOfShot_GettingOffTheShuttleWhileAimedUpwards_FiresSidewaysAgainOnceHeWalks()
    {
        // Arrange. He fired upwards from the shuttle, then got off and walked left.
        Direction aimedFromTheShuttle = BlobWeapon.DirectionOfShot(Direction.Up, true, Direction.Right);

        // Act
        Direction onFoot = BlobWeapon.DirectionOfShot(Direction.Left, false, aimedFromTheShuttle);

        // Assert
        Assert.Equal(Direction.Up, aimedFromTheShuttle);
        Assert.Equal(Direction.Left, onFoot);
    }

    /// <summary>
    ///     And the case that follows from the one above: off the shuttle but standing still, he keeps
    ///     aiming upwards, because nothing has given him a sideways direction since.
    /// </summary>
    [Fact]
    public void DirectionOfShot_GettingOffTheShuttleAndStandingStill_KeepsAimingUpwards()
    {
        // Act
        Direction onFoot = BlobWeapon.DirectionOfShot(Direction.None, false, Direction.Up);

        // Assert
        Assert.Equal(Direction.Up, onFoot);
    }

    /// <summary>
    ///     A shot fired sideways has to clear the item across, and one fired up or down has to
    ///     clear it down - so the gap that counts is the one along the axis it was fired on. This
    ///     measured the horizontal gap whichever way the shot went, which is half of why firing up
    ///     over a collectable killed the shot on the spot.
    /// </summary>
    [Theory]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Right)]
    internal void PointBlankReach_FiredSideways_MeasuresAcrossAndNotDown(Direction firedIn)
    {
        // Arrange - an item well below Blob and barely to the side of him
        var blob = new Rectangle(100, 100, 64, 64);
        var item = new Rectangle(110, 300, 64, 64);

        // Act
        int reach = BlobWeapon.PointBlankReach(blob, item, firedIn);

        // Assert - ten across plus half of Blob, and nothing of the two hundred below
        Assert.Equal(42, reach);
    }

    /// <inheritdoc cref="PointBlankReach_FiredSideways_MeasuresAcrossAndNotDown" />
    [Theory]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Down)]
    internal void PointBlankReach_FiredVertically_MeasuresDownAndNotAcross(Direction firedIn)
    {
        // Arrange - the same two boxes, so only the axis can account for the difference
        var blob = new Rectangle(100, 100, 64, 64);
        var item = new Rectangle(110, 300, 64, 64);

        // Act
        int reach = BlobWeapon.PointBlankReach(blob, item, firedIn);

        // Assert - two hundred down plus half of Blob
        Assert.Equal(232, reach);
    }

    /// <summary>
    ///     The gap is a distance and so has no sign: the item may be either side of Blob, or either
    ///     side of him vertically, and the shot still has as far to go.
    /// </summary>
    [Theory]
    [InlineData(Direction.Left, 60, 100, 72)]
    [InlineData(Direction.Right, 140, 100, 72)]
    [InlineData(Direction.Up, 100, 60, 72)]
    [InlineData(Direction.Down, 100, 140, 72)]
    internal void PointBlankReach_WhicheverSideTheItemIsOn_IsTheSameDistance(
        Direction firedIn,
        int itemX,
        int itemY,
        int expected)
    {
        // Arrange
        var blob = new Rectangle(100, 100, 64, 64);

        // Act
        int reach = BlobWeapon.PointBlankReach(blob, new Rectangle(itemX, itemY, 64, 64), firedIn);

        // Assert
        Assert.Equal(expected, reach);
    }
}
