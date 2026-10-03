// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Display;
using Starquake.Sprites;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The two sums behind a nasty arriving. The shader itself can only be judged by looking at
///     it; these are the numbers handed to it, and the one that matters is the rectangle - a
///     sprite is drawn out of a sheet with its neighbours either side, so a wrong rectangle means
///     the shader samples the nasty next door.
/// </summary>
public class MaterialiseSettingsTests
{
    [Fact]
    public void ProgressThrough_OverTheBirth_RunsFromNothingToAll()
    {
        // Assert
        Assert.Equal(0f, MaterialiseSettings.ProgressThrough(0, 60));
        Assert.Equal(0.5f, MaterialiseSettings.ProgressThrough(30, 60));
        Assert.Equal(1f, MaterialiseSettings.ProgressThrough(60, 60));
    }

    /// <summary>
    ///     The birth runs past its own period - it keeps spinning for up to thirty frames more
    ///     waiting to come to rest square - and a progress past one would start pulling the colour
    ///     channels apart again in the other direction, so an arrived nasty would come apart while
    ///     it waited.
    /// </summary>
    [Theory]
    [InlineData(61)]
    [InlineData(90)]
    [InlineData(1000)]
    public void ProgressThrough_PastTheEndOfTheBirth_StaysAtAll(int frame)
        => Assert.Equal(1f, MaterialiseSettings.ProgressThrough(frame, 60));

    [Fact]
    public void RectIn01_TheFirstSpriteOfASheet_IsTheCornerAndOneCellOfIt()
    {
        // Act - a 256x128 sheet of 64x64 sprites, so four across and two down
        Vector4 rect = SpriteCollection.RectIn01(256, 128, 0, 0, 64, 64);

        // Assert
        Assert.Equal(new Vector4(0f, 0f, 0.25f, 0.5f), rect);
    }

    [Fact]
    public void RectIn01_ASpriteInTheMiddleOfASheet_IsItsOwnCellAndNotItsNeighbours()
    {
        // Act - third across, second down
        Vector4 rect = SpriteCollection.RectIn01(256, 128, 128, 64, 64, 64);

        // Assert
        Assert.Equal(new Vector4(0.5f, 0.5f, 0.25f, 0.5f), rect);
    }

    /// <summary>
    ///     The property the shader's clamp depends on: a sprite's rectangle never runs past the
    ///     edge of the sheet it came out of, whichever cell it is.
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(64, 0)]
    [InlineData(192, 64)]
    public void RectIn01_AnyCell_StaysInsideTheSheet(int xPos, int yPos)
    {
        // Act
        Vector4 rect = SpriteCollection.RectIn01(256, 128, xPos, yPos, 64, 64);

        // Assert
        Assert.InRange(rect.X, 0f, 1f);
        Assert.InRange(rect.Y, 0f, 1f);
        Assert.InRange(rect.X + rect.Z, 0f, 1f);
        Assert.InRange(rect.Y + rect.W, 0f, 1f);
    }
}
