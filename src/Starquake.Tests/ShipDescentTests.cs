// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.Animations;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     Blob's ship falling onto the crash site. The fall needs the crash site and nothing else, so
///     none of this needs the intro sequence that plays it.
/// </summary>
public class ShipDescentTests
{
    [Fact]
    public void Position_BeforeAnyStep_IsOffTheTopOfTheScreenAndToTheLeft()
    {
        // Arrange
        var descent = new ShipDescent(TestData.CrashSite);

        // Assert
        Assert.True(descent.Position.Y < 0);
        Assert.True(descent.Position.X < TestData.CrashSite.X);
    }

    [Fact]
    public void Landed_BeforeAnyStep_IsFalse()
    {
        // Arrange
        var descent = new ShipDescent(TestData.CrashSite);

        // Assert
        Assert.False(descent.Landed);
    }

    /// <summary>
    ///     The drop starts from a standstill, so the first frame slides the ship sideways and does
    ///     not lower it at all. That is what bends the path, rather than running it straight from
    ///     the corner to the crash site.
    /// </summary>
    [Fact]
    public void Step_OnTheFirstFrame_MovesAcrossButDoesNotDrop()
    {
        // Arrange
        var descent = new ShipDescent(TestData.CrashSite);
        Vector2 entry = descent.Position;

        // Act
        descent.Step();

        // Assert
        Assert.True(descent.Position.X > entry.X);
        Assert.Equal(entry.Y, descent.Position.Y);
    }

    /// <summary>
    ///     The sideways rate is paced and never changes, which is the half of the path that is not
    ///     accelerating.
    /// </summary>
    [Fact]
    public void Step_AcrossSeveralFrames_MovesTheSameDistanceSidewaysEachTime()
    {
        // Arrange
        var descent = new ShipDescent(TestData.CrashSite);
        float startX = descent.Position.X;

        // Act
        descent.Step();
        float firstStep = descent.Position.X - startX;
        descent.Step();
        float secondStep = descent.Position.X - startX - firstStep;
        descent.Step();
        float thirdStep = descent.Position.X - startX - firstStep - secondStep;

        // Assert
        Assert.Equal(firstStep, secondStep, TestData.Tolerance);
        Assert.Equal(firstStep, thirdStep, TestData.Tolerance);
    }

    [Fact]
    public void Step_AcrossTheEarlyFrames_DropsFurtherEachFrameThanTheLast()
    {
        // Arrange
        var descent = new ShipDescent(TestData.CrashSite);

        // Act
        descent.Step();
        float beforeSecond = descent.Position.Y;
        descent.Step();
        float secondDrop = descent.Position.Y - beforeSecond;
        descent.Step();
        float thirdDrop = descent.Position.Y - beforeSecond - secondDrop;

        // Assert
        Assert.True(thirdDrop > secondDrop);
    }

    [Fact]
    public void Step_RepeatedlyFromTheTop_EventuallyLands()
    {
        // Arrange
        var descent = new ShipDescent(TestData.CrashSite);

        // Act
        int frames = TestData.StepUntilLanded(descent);

        // Assert
        Assert.True(descent.Landed);
        Assert.True(frames < TestData.MoreFramesThanTheFallNeeds);
    }

    /// <summary>
    ///     The last frame carries the ship past the crash site rather than onto it, because the fall
    ///     is stepped and nothing shortens the final step. That is why the sequence draws the landed
    ///     ship at <see cref="ShipDescent.Rest" /> instead of where it actually stopped.
    /// </summary>
    [Fact]
    public void Position_OnLanding_HasOvershotTheRestingPlace()
    {
        // Arrange
        var descent = new ShipDescent(TestData.CrashSite);

        // Act
        TestData.StepUntilLanded(descent);

        // Assert
        Assert.True(descent.Position.Y > descent.Rest.Y);
    }

    /// <summary>
    ///     The overshoot stays within one frame's travel. Without the full-speed approach near the
    ///     ground, the acceleration would keep building and the ship would arrive a long way below
    ///     the site it was aimed at.
    /// </summary>
    [Fact]
    public void Position_OnLanding_HasOvershotByLessThanOneFramesTravel()
    {
        // Arrange
        var descent = new ShipDescent(TestData.CrashSite);
        float previousY = descent.Position.Y;

        // Act
        while (!descent.Landed)
        {
            previousY = descent.Position.Y;
            descent.Step();
        }

        // Assert
        Assert.True(descent.Position.Y - descent.Rest.Y < descent.Position.Y - previousY);
    }

    [Fact]
    public void Rest_Always_IsWhereTheDescentWasAimed()
    {
        // Arrange
        var descent = new ShipDescent(TestData.CrashSite);

        // Act
        TestData.StepUntilLanded(descent);

        // Assert
        Assert.Equal(TestData.CrashSite, descent.Rest);
    }

    /// <summary>
    ///     A crash site on the far left, so the ship travels backwards across the screen rather than
    ///     forwards. The sideways rate is a difference rather than a magnitude, so it signs itself.
    /// </summary>
    [Fact]
    public void Step_TowardASiteBehindTheEntryPoint_MovesBackwardsAcross()
    {
        // Arrange
        var descent = new ShipDescent(TestData.CrashSiteToTheLeftOfEntry);
        float startX = descent.Position.X;

        // Act
        descent.Step();

        // Assert
        Assert.True(descent.Position.X < startX);
    }

    [Fact]
    public void Step_TowardASiteBehindTheEntryPoint_StillLands()
    {
        // Arrange
        var descent = new ShipDescent(TestData.CrashSiteToTheLeftOfEntry);

        // Act
        TestData.StepUntilLanded(descent);

        // Assert
        Assert.True(descent.Landed);
    }

    private static class TestData
    {

        public const int MoreFramesThanTheFallNeeds = 1000;

        public const int Tolerance = 4;
        /// <summary>
        ///     A plausible crash site: well down the screen and well to the right of where the ship
        ///     comes in.
        /// </summary>
        public static readonly Point CrashSite = new(900, 500);

        public static readonly Point CrashSiteToTheLeftOfEntry = new(40, 500);

        /// <summary>
        ///     Steps until the ship is down, and says how many frames that took. Gives up at
        ///     <see cref="MoreFramesThanTheFallNeeds" /> rather than spinning, so a fall that cannot
        ///     land fails an assertion instead of hanging the suite.
        /// </summary>
        public static int StepUntilLanded(ShipDescent descent)
        {
            int frames = 0;

            while (!descent.Landed && frames < MoreFramesThanTheFallNeeds)
            {
                descent.Step();
                frames++;
            }

            return frames;
        }
    }
}
