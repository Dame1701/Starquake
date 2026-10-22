// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Constants;
using Starquake.Display;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The hotkey message's fade. All of it is counted in logic frames, so all of it can be
///     checked by stepping <c>Update</c> and watching the level - no graphics device, no font.
/// </summary>
public class ModeAnnouncementTests
{
    /// <summary>Long enough that the fade has finished, whatever the timings are tuned to.</summary>
    private const int LongEnoughForAnything = LogicClock.FramesPerSecond * 10;

    [Fact]
    public void WithNothingEverShown_StaysAtNothing()
    {
        // Arrange
        var announcement = new ModeAnnouncement();

        // Act
        Step(announcement, LongEnoughForAnything);

        // Assert
        Assert.Equal(ColourLevels.None, announcement.Fade);
    }

    [Fact]
    public void Shown_FadesUpToFullAndStaysThereWhileItIsHeld()
    {
        // Arrange
        var announcement = new ModeAnnouncement();

        // Act
        announcement.Show("CRT SCANLINES");

        int afterOneFrame = StepAndRead(announcement, 1);
        int onceUp = StepAndRead(announcement, LogicClock.Seconds(0.5));

        // Assert
        Assert.True(afterOneFrame > ColourLevels.None);
        Assert.True(afterOneFrame < ColourLevels.Full);
        Assert.Equal(ColourLevels.Full, onceUp);
    }

    [Fact]
    public void Shown_FadesBackToNothingOnItsOwnAndStopsThere()
    {
        // Arrange
        var announcement = new ModeAnnouncement();
        announcement.Show("ORIGINAL GRAPHICS");

        // Act
        Step(announcement, LongEnoughForAnything);

        // Assert
        Assert.Equal(ColourLevels.None, announcement.Fade);
    }

    /// <summary>
    ///     It arrives faster than it leaves, which is what makes it read as a response to the key
    ///     rather than as something drifting past.
    /// </summary>
    [Fact]
    public void Shown_ComesUpFasterThanItGoesAway()
    {
        // Arrange
        var announcement = new ModeAnnouncement();
        announcement.Show("BACKDROPS ON");

        // Act
        int framesToArrive = FramesUntil(announcement, () => announcement.Fade == ColourLevels.Full);

        _ = FramesUntil(announcement, () => announcement.Fade < ColourLevels.Full);

        int framesToLeave = FramesUntil(announcement, () => announcement.Fade == ColourLevels.None);

        // Assert
        Assert.True(
            framesToLeave > framesToArrive,
            $"arrived in {framesToArrive} frames and left in {framesToLeave}");
    }

    /// <summary>
    ///     Leaning on a hotkey has to read as one message changing rather than as a queue of them,
    ///     so showing again restarts the hold from full rather than adding to it.
    /// </summary>
    [Fact]
    public void ShownAgainWhileItIsStillGoing_StartsTheHoldOver()
    {
        // Arrange - most of the way through the first message
        var announcement = new ModeAnnouncement();
        announcement.Show("CRT SCANLINES");
        Step(announcement, LogicClock.Seconds(1.2));

        // Act
        announcement.Show("CRT PHOSPHOR");
        Step(announcement, LogicClock.Seconds(1.0));

        // Assert - still at full a second later, which the first message would not have been
        Assert.Equal(ColourLevels.Full, announcement.Fade);
    }

    /// <summary>
    ///     The fade must not run past nought and come back round, which is what an unclamped
    ///     subtraction would do to a byte.
    /// </summary>
    [Fact]
    public void Shown_NeverGoesBelowNothingOrAboveFull()
    {
        // Arrange
        var announcement = new ModeAnnouncement();
        announcement.Show("CRT FULL TUBE");

        // Act / Assert
        for (int frame = 0; frame < LongEnoughForAnything; frame++)
        {
            announcement.Update();

            Assert.InRange(announcement.Fade, ColourLevels.None, ColourLevels.Full);
        }
    }

    /// <summary>
    ///     Steps the fade until something is true, and says how many frames that took. Gives up at
    ///     <see cref="LongEnoughForAnything" /> so a rule that never comes true fails on the
    ///     assertion rather than hanging the test run.
    /// </summary>
    private static int FramesUntil(ModeAnnouncement announcement, Func<bool> isTrue)
    {
        int frames = 0;

        while (!isTrue() && frames < LongEnoughForAnything)
        {
            announcement.Update();
            frames++;
        }

        return frames;
    }

    private static void Step(ModeAnnouncement announcement, int frames)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            announcement.Update();
        }
    }

    private static int StepAndRead(ModeAnnouncement announcement, int frames)
    {
        Step(announcement, frames);

        return announcement.Fade;
    }
}
