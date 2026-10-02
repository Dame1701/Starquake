// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.ActiveSprites;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     Which way the blob's shot leaves him. <see cref="BlobFire" /> and <see cref="IFireSurroundings" />
///     already cover where a shot goes once it is flying; this is the decision taken as it is launched,
///     and it was unreachable while it sat inside <c>Blob.StartFiring</c>.
/// </summary>
/// <remarks>
///     Only this rule is covered, and that is the honest limit of the extraction: the rest of
///     <see cref="BlobWeapon" /> pulls two particle effects and a sprite sheet out of the resource
///     manager before it exists, so the class cannot be constructed without the content pipeline. The
///     plan for this stage claimed otherwise - see <c>GODCLASSES.md</c>.
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
    ///     The reason the last direction is carried rather than recomputed: a blob standing still has no
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
    ///     takes the direction of travel whatever it is, and a blob on a shuttle that is not moving has
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
}
