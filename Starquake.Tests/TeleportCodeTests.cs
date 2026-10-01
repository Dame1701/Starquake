// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using MessagePack;
using Starquake.SharedLibs;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The rules a teleport destination code has to follow, and the codes the planet shipped with.
/// </summary>
/// <remarks>
///     A code is authored in the screen editor and carried in the screen data, so these are the only
///     two things standing between a typo in the editor and a teleporter that cannot be arrived at:
///     a code the booth's keypad could not spell, and two screens answering to the same one.
/// </remarks>
public class TeleportCodeTests
{
    [Theory]
    [InlineData("groin")]
    [InlineData("GROIN")]
    [InlineData("Groin")]
    public void IsWellFormed_FiveLetters_IsAccepted(string code)
        => Assert.True(TeleportCodes.IsWellFormed(code));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("groi")]
    [InlineData("groins")]
    [InlineData("gro n")]
    [InlineData("gro1n")]
    [InlineData("gro-n")]
    public void IsWellFormed_AnythingTheKeypadCouldNotSpell_IsRefused(string? code)
        => Assert.False(TeleportCodes.IsWellFormed(code));

    [Fact]
    public void Normalise_ACodeTypedInTheEditor_IsUpperCasedAndTrimmed()
        => Assert.Equal("GROIN", TeleportCodes.Normalise("  GroIn "));

    [Fact]
    public void Original_EveryCode_IsOneTheBoothCouldSpell()
        => Assert.All(TeleportCodes.Original.Values, code => Assert.True(TeleportCodes.IsWellFormed(code)));

    /// <summary>
    ///     The game takes the first screen whose code matches, so a code used twice would leave the
    ///     second of the two screens unreachable.
    /// </summary>
    [Fact]
    public void Original_NoTwoScreens_ShareACode()
        => Assert.Equal(TeleportCodes.Original.Count, TeleportCodes.Original.Values.Distinct().Count());

    [Fact]
    public void TeleportCode_SerialisedWithAScreen_SurvivesTheRoundTrip()
    {
        // Arrange
        var screen = new GameScreenData(1, 1)
        {
            TeleportCode = "GROIN"
        };

        // Act
        GameScreenData restored = MessagePackSerializer.Deserialize<GameScreenData>(
            MessagePackSerializer.Serialize(screen));

        // Assert
        Assert.Equal("GROIN", restored.TeleportCode);
    }

    /// <summary>
    ///     A map saved before codes were part of a screen holds two values per screen rather than
    ///     three. It still has to load, because that is every copy of the map that exists until the
    ///     editor saves one again - and when it does load, the screen has no code rather than a
    ///     broken one.
    /// </summary>
    [Fact]
    public void TeleportCode_AScreenSavedBeforeCodesWereStored_LoadsWithoutOne()
    {
        // Arrange - the screen as the old format wrote it: tiles, collision map, and nothing else
        byte[] saved = MessagePackSerializer.ConvertFromJson("[null,[]]");

        // Act
        GameScreenData restored = MessagePackSerializer.Deserialize<GameScreenData>(saved);

        // Assert
        Assert.Null(restored.TeleportCode);
    }
}
