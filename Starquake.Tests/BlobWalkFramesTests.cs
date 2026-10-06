// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.ActiveSprites;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     Blob's walk, and the pose he picks it up at when he turns round mid-stride. Two comparisons
///     and eight numbers, all of which were private to <c>Blob</c>.
/// </summary>
/// <remarks>
///     Half of these pin the sheet's layout rather than the rule, which is the point: the rule only
///     produces a matching pose because each transition frame sits immediately beside the run it is
///     turning away from. Renumbering the sheet would not fail to compile and would not look wrong
///     in a diff.
/// </remarks>
public class BlobWalkFramesTests
{
    /// <summary>
    ///     The layout the turn rule depends on: left run, turn-to-right, resting, turn-to-left,
    ///     right run, in that order and with no gaps and no overlap.
    /// </summary>
    [Fact]
    public void TheSheet_IsLaidOutAsElevenFramesInOneRun()
    {
        Assert.Equal(0, BlobWalkFrames.WalkLeftFirst);
        Assert.Equal(BlobWalkFrames.WalkLeftLast + 1, BlobWalkFrames.TurnToRight);
        Assert.Equal(BlobWalkFrames.TurnToRight + 1, BlobWalkFrames.Resting);
        Assert.Equal(BlobWalkFrames.Resting + 1, BlobWalkFrames.TurnToLeft);
        Assert.Equal(BlobWalkFrames.TurnToLeft + 1, BlobWalkFrames.WalkRightFirst);
    }

    /// <summary>
    ///     Both runs are the same length, because they are the same walk drawn facing either way.
    /// </summary>
    [Fact]
    public void TheTwoWalkRuns_AreTheSameLength()
        => Assert.Equal(
            BlobWalkFrames.WalkLeftLast - BlobWalkFrames.WalkLeftFirst,
            BlobWalkFrames.WalkRightLast - BlobWalkFrames.WalkRightFirst);

    /// <summary>
    ///     Each transition frame is on the far side of resting from the run it turns towards, which
    ///     is what makes one comparison enough to decide a turn.
    /// </summary>
    [Fact]
    public void EachTransitionFrame_SitsBetweenRestingAndTheRunItTurnsAwayFrom()
    {
        Assert.InRange(BlobWalkFrames.TurnToRight, BlobWalkFrames.WalkLeftLast, BlobWalkFrames.Resting);
        Assert.InRange(BlobWalkFrames.TurnToLeft, BlobWalkFrames.Resting, BlobWalkFrames.WalkRightFirst);
    }

    // --- turning left ---------------------------------------------------------------------------

    /// <summary>
    ///     Coming out of the right-hand run, every frame of it drops back to the one transition pose
    ///     rather than to the start of the left run - which is the whole point of the rule.
    /// </summary>
    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    public void TurningLeftFrom_AnyRightWalkFrame_PicksUpAtTheTransitionPose(int frame)
        => Assert.Equal(BlobWalkFrames.TurnToLeft, BlobWalkFrames.TurningLeftFrom(frame));

    /// <summary>
    ///     Already facing left, or standing still, he carries on from where he is. Snapping here is
    ///     what the rule exists to stop.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void TurningLeftFrom_AFrameThatIsNotTheRightWalk_CarriesOnFromIt(int frame)
        => Assert.Equal(frame, BlobWalkFrames.TurningLeftFrom(frame));

    // --- turning right --------------------------------------------------------------------------

    /// <inheritdoc cref="TurningLeftFrom_AnyRightWalkFrame_PicksUpAtTheTransitionPose" />
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void TurningRightFrom_AnyLeftWalkFrame_PicksUpAtTheTransitionPose(int frame)
        => Assert.Equal(BlobWalkFrames.TurnToRight, BlobWalkFrames.TurningRightFrom(frame));

    /// <inheritdoc cref="TurningLeftFrom_AFrameThatIsNotTheRightWalk_CarriesOnFromIt" />
    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(10)]
    public void TurningRightFrom_AFrameThatIsNotTheLeftWalk_CarriesOnFromIt(int frame)
        => Assert.Equal(frame, BlobWalkFrames.TurningRightFrom(frame));

    // --- the pair together ----------------------------------------------------------------------

    /// <summary>
    ///     Standing still is the one pose both turns leave alone, which is why Blob walking off from
    ///     rest starts from the resting frame in either direction rather than jerking first.
    /// </summary>
    [Fact]
    public void BothTurns_FromResting_LeaveHimStanding()
    {
        Assert.Equal(BlobWalkFrames.Resting, BlobWalkFrames.TurningLeftFrom(BlobWalkFrames.Resting));
        Assert.Equal(BlobWalkFrames.Resting, BlobWalkFrames.TurningRightFrom(BlobWalkFrames.Resting));
    }

    /// <summary>
    ///     And every frame of the sheet comes back inside the sheet, whichever way he turns - so no
    ///     turn can ever ask for a sprite the run does not have.
    /// </summary>
    [Fact]
    public void BothTurns_FromEveryFrameOfTheSheet_StayWithinIt()
    {
        for (int frame = BlobWalkFrames.WalkLeftFirst; frame <= BlobWalkFrames.WalkRightLast; frame++)
        {
            Assert.InRange(BlobWalkFrames.TurningLeftFrom(frame),
                BlobWalkFrames.WalkLeftFirst, BlobWalkFrames.WalkRightLast);
            Assert.InRange(BlobWalkFrames.TurningRightFrom(frame),
                BlobWalkFrames.WalkLeftFirst, BlobWalkFrames.WalkRightLast);
        }
    }
}
