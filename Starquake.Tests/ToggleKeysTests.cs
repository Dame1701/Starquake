// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Input;
using Starquake.Input;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The labels the define-keys screen shows. A pure lookup, and the only part of
///     <see cref="ToggleKeys" /> that can be tested without a keyboard attached.
/// </summary>
public class ToggleKeysTests
{
    [Theory]
    [InlineData(Keys.NumPad0, "N0")]
    [InlineData(Keys.NumPad9, "N9")]
    [InlineData(Keys.Space, "SP")]
    [InlineData(Keys.LeftControl, "LC")]
    [InlineData(Keys.Escape, "ES")]
    [InlineData(Keys.OemBackslash, @"\")]
    [InlineData(Keys.OemQuotes, "\"")]
    [InlineData(Keys.OemTilde, "@")]
    public void GetFriendlyName_AKeyWithALabel_GivesTheLabel(Keys key, string expected)
        => Assert.Equal(expected, ToggleKeys.GetFriendlyName(key));

    [Fact]
    public void GetFriendlyName_AKeyWithoutOne_GivesItsOwnName()
        => Assert.Equal("A", ToggleKeys.GetFriendlyName(Keys.A));

    /// <summary>
    ///     Three of the four arrows are abbreviated and Up is not, so it reads by its own name
    ///     beside DN, LF and RT. That is what the screen has always shown; this pins it so that
    ///     tidying the table cannot quietly change it.
    /// </summary>
    [Fact]
    public void GetFriendlyName_TheArrowKeys_AbbreviateAllButUp()
    {
        Assert.Equal("DN", ToggleKeys.GetFriendlyName(Keys.Down));
        Assert.Equal("LF", ToggleKeys.GetFriendlyName(Keys.Left));
        Assert.Equal("RT", ToggleKeys.GetFriendlyName(Keys.Right));
        Assert.Equal("Up", ToggleKeys.GetFriendlyName(Keys.Up));
    }

    /// <summary>
    ///     Four labels are used by two keys each. Pinned because the define-keys screen has only two
    ///     characters to say which key was chosen, so these pairs are genuinely indistinguishable on
    ///     it - which is worth knowing deliberately rather than discovering.
    /// </summary>
    [Fact]
    public void GetFriendlyName_FourLabels_AreSharedByTwoKeysEach()
    {
        Assert.Equal(ToggleKeys.GetFriendlyName(Keys.Right), ToggleKeys.GetFriendlyName(Keys.Enter));
        Assert.Equal(ToggleKeys.GetFriendlyName(Keys.BrowserSearch), ToggleKeys.GetFriendlyName(Keys.BrowserStop));
        Assert.Equal(ToggleKeys.GetFriendlyName(Keys.CapsLock), ToggleKeys.GetFriendlyName(Keys.OemClear));
        Assert.Equal(ToggleKeys.GetFriendlyName(Keys.Decimal), ToggleKeys.GetFriendlyName(Keys.OemPeriod));
    }

    [Fact]
    public void GetFriendlyName_EveryLabel_FitsTheTwoCharactersTheScreenHas()
    {
        foreach (Keys key in Enum.GetValues<Keys>())
        {
            string label = ToggleKeys.GetFriendlyName(key);

            // A key with no label of its own falls back to its name, which may be any length
            if (label != key.ToString())
            {
                Assert.InRange(label.Length, 1, 2);
            }
        }
    }
}
