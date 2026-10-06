// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.Core;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The transform that puts the composed picture on the back buffer. It used to be computed
///     inside <c>GameEngine</c> from <c>GraphicsDevice.PresentationParameters</c>, so checking it
///     meant standing up a graphics device; on <see cref="PresentationSettings" /> it is
///     arithmetic on two integers.
///     <para>
///         What these pin is the promise the game is composed on: the scale is uniform, so the
///         picture is never stretched whatever shape the window is, and whatever is left over
///         becomes an even border on the two opposite sides rather than being taken off one.
///     </para>
/// </summary>
public class PresentationSettingsTests
{
    [Fact]
    public void TransformFor_ABackBufferTheCompositionSize_ScalesByOneAndLeavesNoBorder()
    {
        // Act
        Matrix transform = PresentationSettings.TransformFor(TestData.CompositionWidth, TestData.CompositionHeight);

        // Assert
        Assert.Equal(1f, transform.M11);
        Assert.Equal(1f, transform.M22);
        Assert.Equal(0f, transform.M41);
        Assert.Equal(0f, transform.M42);
    }

    [Fact]
    public void TransformFor_ABackBufferTwiceTheCompositionInBothAxes_ScalesByTwoAndLeavesNoBorder()
    {
        // Act
        Matrix transform = PresentationSettings.TransformFor(
            TestData.CompositionWidth * 2,
            TestData.CompositionHeight * 2);

        // Assert
        Assert.Equal(2f, transform.M11);
        Assert.Equal(2f, transform.M22);
        Assert.Equal(0f, transform.M41);
        Assert.Equal(0f, transform.M42);
    }

    [Fact]
    public void TransformFor_ABackBufferWiderThanTheCompositionsShape_IsLimitedByHeightAndBordersTheSides()
    {
        // Arrange
        // Twice the height, but four times the width: the height is what runs out first.
        int backBufferWidth = TestData.CompositionWidth * 4;
        int backBufferHeight = TestData.CompositionHeight * 2;

        // Act
        Matrix transform = PresentationSettings.TransformFor(backBufferWidth, backBufferHeight);

        // Assert
        Assert.Equal(2f, transform.M11);
        Assert.Equal(2f, transform.M22);
        Assert.Equal((backBufferWidth - TestData.CompositionWidth * 2f) / 2f, transform.M41);
        Assert.Equal(0f, transform.M42);
    }

    [Fact]
    public void TransformFor_ABackBufferTallerThanTheCompositionsShape_IsLimitedByWidthAndBordersTopAndBottom()
    {
        // Arrange
        int backBufferWidth = TestData.CompositionWidth * 2;
        int backBufferHeight = TestData.CompositionHeight * 4;

        // Act
        Matrix transform = PresentationSettings.TransformFor(backBufferWidth, backBufferHeight);

        // Assert
        Assert.Equal(2f, transform.M11);
        Assert.Equal(2f, transform.M22);
        Assert.Equal(0f, transform.M41);
        Assert.Equal((backBufferHeight - TestData.CompositionHeight * 2f) / 2f, transform.M42);
    }

    [Fact]
    public void TransformFor_ABackBufferSmallerThanTheComposition_ScalesDownRatherThanClipping()
    {
        // Act
        Matrix transform = PresentationSettings.TransformFor(
            TestData.CompositionWidth / 2,
            TestData.CompositionHeight / 2);

        // Assert
        Assert.Equal(0.5f, transform.M11);
        Assert.Equal(0.5f, transform.M22);
    }

    [Fact]
    public void TransformFor_AnyBackBuffer_ScalesBothAxesByTheSameAmount()
    {
        // Arrange
        // A shape that divides evenly into neither axis, which is what a dragged window looks like.
        const int AwkwardWidth = 1913;
        const int AwkwardHeight = 1071;

        // Act
        Matrix transform = PresentationSettings.TransformFor(AwkwardWidth, AwkwardHeight);

        // Assert
        Assert.Equal(transform.M11, transform.M22);
    }

    [Fact]
    public void TransformFor_AnyBackBuffer_SplitsWhatIsLeftOverEvenlyBetweenTheTwoBorders()
    {
        // Arrange
        const int AwkwardWidth = 1913;
        const int AwkwardHeight = 1071;

        // Act
        Matrix transform = PresentationSettings.TransformFor(AwkwardWidth, AwkwardHeight);

        // Assert
        // The picture plus a border on each side is the whole back buffer, so twice either border
        // accounts for exactly the part the picture does not cover.
        Assert.Equal(AwkwardWidth - TestData.CompositionWidth * transform.M11, transform.M41 * 2f, 3);
        Assert.Equal(AwkwardHeight - TestData.CompositionHeight * transform.M22, transform.M42 * 2f, 3);
    }

    [Fact]
    public void Recalculate_ForABackBuffer_LeavesTransformAtWhatTransformForWouldGive()
    {
        // Arrange
        var settings = new PresentationSettings();

        // Act
        settings.Recalculate(TestData.CompositionWidth * 2, TestData.CompositionHeight * 2);

        // Assert
        Assert.Equal(
            PresentationSettings.TransformFor(TestData.CompositionWidth * 2, TestData.CompositionHeight * 2),
            settings.Transform);
    }

    [Fact]
    public void Transform_BeforeAnyRecalculation_IsIdentitySoNothingDrawnEarlyIsMoved()
    {
        // Act
        var settings = new PresentationSettings();

        // Assert
        Assert.Equal(Matrix.Identity, settings.Transform);
    }

    private static class TestData
    {
        /// <summary>
        ///     The fixed size the game composes at, which every scale here is measured against.
        /// </summary>
        public const int CompositionWidth = CompositionSize.Width;

        /// <inheritdoc cref="CompositionWidth" />
        public const int CompositionHeight = CompositionSize.Height;
    }
}
