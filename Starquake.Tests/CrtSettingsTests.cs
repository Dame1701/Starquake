// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Core;
using Starquake.Tuning;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The arithmetic behind the cathode ray tube. The shader itself can only be judged by looking
///     at it, but the numbers it is driven by are ordinary sums and this is where they are held.
/// </summary>
/// <remarks>
///     The one that matters is <see cref="CrtSettings.ScanlinePeriodFor" />. The overlay this
///     replaced was a two-pixel pattern scaled by a fraction, and that is what made it a grey wash
///     with a beat running through it; a period that is always a whole number of output pixels is
///     the whole of the fix, and a regression there would look like the bug coming back rather
///     than like anything failing.
/// </remarks>
public class CrtSettingsTests
{
    [Fact]
    public void Next_PressedFourTimes_AddsOneThingAtATimeAndThenTakesTheLotAway()
    {
        // Act
        CrtMode first = CrtSettings.Next(CrtMode.None);
        CrtMode second = CrtSettings.Next(first);
        CrtMode third = CrtSettings.Next(second);
        CrtMode fourth = CrtSettings.Next(third);

        // Assert
        Assert.Equal(CrtMode.Scanlines, first);
        Assert.Equal(CrtMode.Phosphor, second);
        Assert.Equal(CrtMode.Tube, third);
        Assert.Equal(CrtMode.None, fourth);
    }

    /// <summary>
    ///     Every mode is reachable and the cycle closes, whichever one it is started from. Worth
    ///     pinning separately: a mode added to the enum and forgotten here would be a step the key
    ///     could never reach.
    /// </summary>
    [Fact]
    public void Next_FromAnyMode_ReachesEveryOtherModeAndComesBack()
    {
        foreach (CrtMode start in Enum.GetValues<CrtMode>())
        {
            // Act
            List<CrtMode> visited = [];
            CrtMode mode = start;

            for (int press = 0; press < Enum.GetValues<CrtMode>().Length; press++)
            {
                mode = CrtSettings.Next(mode);
                visited.Add(mode);
            }

            // Assert
            Assert.Equal(start, mode);
            Assert.Equal(Enum.GetValues<CrtMode>().Length, visited.Distinct().Count());
        }
    }

    /// <summary>
    ///     The scales the game actually runs at, none of which is a whole number - which is the
    ///     reason the bitmap could not be kept.
    /// </summary>
    [Theory]
    [InlineData(1.0f, 2)]
    [InlineData(1.35f, 3)]
    [InlineData(1.8f, 4)]
    [InlineData(2.3625f, 5)]
    [InlineData(2.7f, 5)]
    public void ScanlinePeriodFor_AtTheScalesTheGameRunsAt_IsAWholeNumberOfOutputPixels(
        float scale,
        int expected)
        => Assert.Equal(expected, CrtSettings.ScanlinePeriodFor(scale));

    /// <summary>
    ///     One pixel on and one off is the thinnest a scanline can be, so a window smaller than the
    ///     composition cannot be allowed to ask for less.
    /// </summary>
    [Theory]
    [InlineData(0.1f)]
    [InlineData(0.5f)]
    [InlineData(0.9f)]
    public void ScanlinePeriodFor_AWindowSmallerThanTheComposition_StillGetsTwoPixels(float scale)
        => Assert.Equal(2, CrtSettings.ScanlinePeriodFor(scale));

    [Fact]
    public void GainFor_WithTheGrilleOn_PutsBackMoreLightThanScanlinesAlone()
    {
        // Act
        float scanlines = CrtSettings.GainFor(CrtMode.Scanlines);
        float phosphor = CrtSettings.GainFor(CrtMode.Phosphor);

        // Assert - each pattern costs half its depth on average, so the gain is 1 / what is left
        Assert.Equal(1 / (1 - CrtSettings.ScanlineDepth / 2), scanlines, 5);
        Assert.Equal(
            1 / ((1 - CrtSettings.ScanlineDepth / 2) * (1 - CrtSettings.MaskDepth / 2)),
            phosphor,
            5);
        Assert.Equal(CrtSettings.GainFor(CrtMode.Phosphor), CrtSettings.GainFor(CrtMode.Tube));
    }

    /// <summary>
    ///     A gain that did not put back at least what the patterns take out would leave the picture
    ///     dimmer for being switched on, which is the thing it exists to stop.
    /// </summary>
    [Theory]
    [InlineData(CrtMode.Scanlines)]
    [InlineData(CrtMode.Phosphor)]
    [InlineData(CrtMode.Tube)]
    internal void GainFor_EveryModeThatDarkens_BrightensToCompensate(CrtMode mode)
        => Assert.True(CrtSettings.GainFor(mode) > 1);

    [Fact]
    public void CompositionRectFor_ABufferTheCompositionExactlyFills_IsTheWholeBuffer()
    {
        // Act
        Vector4 rect = CrtSettings.CompositionRectFor(1280, 800);

        // Assert
        Assert.Equal(new Vector4(0, 0, 1, 1), rect);
    }

    /// <summary>
    ///     A buffer wider than the composition's shape letterboxes left and right, and the shader
    ///     has to bow the picture about the middle of the picture rather than the middle of the
    ///     window - so the rect has to say where the picture actually is.
    /// </summary>
    [Fact]
    public void CompositionRectFor_AWiderBuffer_SitsTheCompositionInTheMiddleOfIt()
    {
        // Act
        Vector4 rect = CrtSettings.CompositionRectFor(2560, 800);

        // Assert - half the width, a quarter in from the left, and the full height
        Assert.Equal(new Vector4(0.25f, 0, 0.5f, 1), rect);
    }

    [Fact]
    public void CompositionRectFor_ATallerBuffer_SitsTheCompositionInTheMiddleOfIt()
    {
        // Act
        Vector4 rect = CrtSettings.CompositionRectFor(1280, 1600);

        // Assert
        Assert.Equal(new Vector4(0, 0.25f, 1, 0.5f), rect);
    }

    /// <summary>
    ///     The rect and the transform the rest of the game is drawn through have to agree about
    ///     where the picture is, or the curve would bow it about a point it is not centred on.
    /// </summary>
    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(2560, 1440)]
    [InlineData(3456, 2234)]
    internal void CompositionRectFor_AnyBuffer_AgreesWithThePresentationTransform(
        int bufferWidth,
        int bufferHeight)
    {
        // Act
        Vector4 rect = CrtSettings.CompositionRectFor(bufferWidth, bufferHeight);
        Matrix transform = PresentationSettings.TransformFor(bufferWidth, bufferHeight);

        // Assert - the transform's corner and scale, expressed as a fraction of the buffer
        Assert.Equal(transform.Translation.X / bufferWidth, rect.X, 5);
        Assert.Equal(transform.Translation.Y / bufferHeight, rect.Y, 5);
        Assert.Equal(CrtSettings.ScaleFor(bufferWidth, bufferHeight), transform.M11, 5);
    }

    /// <summary>
    ///     The property the whole decay exists for: the streak has to be the same length in
    ///     seconds whatever rate the display runs at. Decaying twice over half an interval has to
    ///     leave exactly what decaying once over the whole interval leaves, or a 144 hertz monitor
    ///     would show a shorter trail than a 60 hertz one.
    /// </summary>
    [Theory]
    [InlineData(1.0 / 60)]
    [InlineData(1.0 / 120)]
    [InlineData(1.0 / 144)]
    public void AfterglowRetainedOver_HalfAnIntervalTwice_LeavesWhatTheWholeIntervalLeaves(
        double seconds)
    {
        // Act
        float inOneStep = CrtSettings.AfterglowRetainedOver(seconds);
        float inTwoSteps = CrtSettings.AfterglowRetainedOver(seconds / 2)
            * CrtSettings.AfterglowRetainedOver(seconds / 2);

        // Assert
        Assert.Equal(inOneStep, inTwoSteps, 5);
    }

    [Fact]
    public void AfterglowRetainedOver_TheDecayTime_LeavesTheUsualFraction()
    {
        // Act
        float retained = CrtSettings.AfterglowRetainedOver(CrtSettings.PhosphorDecaySeconds);

        // Assert - one time constant is 1/e, which is where "about a third" comes from
        Assert.Equal(1 / Math.E, retained, 5);
    }

    [Fact]
    public void AfterglowRetainedOver_ALongerGap_LeavesLess()
    {
        // Act
        float quick = CrtSettings.AfterglowRetainedOver(1.0 / 144);
        float slow = CrtSettings.AfterglowRetainedOver(1.0 / 30);

        // Assert
        Assert.True(quick > slow);
        Assert.InRange(quick, 0, 1);
        Assert.InRange(slow, 0, 1);
    }

    /// <summary>
    ///     A stalled frame must not bring the last picture back at full strength, which is what an
    ///     unclamped fraction would do if it ever went above one.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(10.0)]
    public void AfterglowRetainedOver_AnyGapAtAll_StaysBetweenNothingAndEverything(double seconds)
        => Assert.InRange(CrtSettings.AfterglowRetainedOver(seconds), 0f, 1f);

    /// <summary>
    ///     Each press of F9 adds one thing, and the persistence arrives with the phosphor rather
    ///     than with the scanlines.
    /// </summary>
    [Theory]
    [InlineData(CrtMode.None, false)]
    [InlineData(CrtMode.Scanlines, false)]
    [InlineData(CrtMode.Phosphor, true)]
    [InlineData(CrtMode.Tube, true)]
    internal void ShowsAfterglow_IsTheModesWithAPhosphorInThem(CrtMode mode, bool expected)
        => Assert.Equal(expected, CrtSettings.ShowsAfterglow(mode));

    [Theory]
    [InlineData(CrtMode.Scanlines, "Scanlines")]
    [InlineData(CrtMode.Phosphor, "Phosphor")]
    [InlineData(CrtMode.Tube, "Tube")]
    internal void TechniqueFor_EveryModeThatDraws_NamesItsOwnPassOfTheShader(
        CrtMode mode,
        string expected)
        => Assert.Equal(expected, CrtSettings.TechniqueFor(mode));
}
