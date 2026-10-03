// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using MessagePack;
using Starquake.Constants;
using Starquake.SharedLibs;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     What a signpost reads, and what happens to it when the teleporter it names changes.
/// </summary>
/// <remarks>
///     The board the artwork draws is blank, so the only thing that puts a code on a sign is
///     <see cref="Signposts.CodeShownOn" />. Everything it could be asked about a half-built map -
///     a sign pointed nowhere, a sign pointed at a screen that has lost its code - has to come back
///     as a blank board rather than as a crash or as a code no booth answers to.
/// </remarks>
public class SignpostTests
{

    [Fact]
    public void CodeShownOn_ASignPointedAtATeleporter_ReadsThatTeleportersCode()
    {
        // Arrange
        GameScreenData[] screens = Planet(signposted: 1);
        screens[1].TeleportCode = "PALMA";

        // Act
        string? code = Signposts.CodeShownOn(screens, 0);

        // Assert
        Assert.Equal("PALMA", code);
    }

    /// <summary>
    ///     The whole reason a sign stores a screen number rather than a copy of the code: renaming
    ///     the code has to rename what every sign pointing at it says.
    /// </summary>
    [Fact]
    public void CodeShownOn_TheTeleportersCodeRenamed_ReadsTheNewCode()
    {
        // Arrange
        GameScreenData[] screens = Planet(signposted: 1);
        screens[1].TeleportCode = "PALMA";

        // Act
        screens[1].TeleportCode = "URAIQ";

        // Assert
        Assert.Equal("URAIQ", Signposts.CodeShownOn(screens, 0));
    }

    [Fact]
    public void CodeShownOn_ASignPointedNowhere_ReadsNothing()
        => Assert.Null(Signposts.CodeShownOn(Planet(signposted: null), 0));

    [Fact]
    public void CodeShownOn_ASignPointedAtAScreenWithNoCode_ReadsNothing()
        => Assert.Null(Signposts.CodeShownOn(Planet(signposted: 1), 0));

    /// <summary>
    ///     A screen number is only as good as the map it was saved against, and a sign pointed off
    ///     the end of a smaller one has to stand blank rather than take the game down with it.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void CodeShownOn_ASignPointedOffTheMap_ReadsNothing(int signposted)
        => Assert.Null(Signposts.CodeShownOn(Planet(signposted), 0));

    [Fact]
    public void StandOn_AScreenWithASignpostTileOnIt_IsSaidToHaveOne()
    {
        // Arrange
        var screen = new GameScreenData(TileMetrics.TilesAcross, TileMetrics.TilesDown);

        // Act
        screen.ScreenTiles[3, 4].TileImage = (int)ActionTiles.TeleportSignpost;

        // Assert
        Assert.True(Signposts.StandOn(screen));
    }

    [Fact]
    public void StandOn_AScreenWithNoSignpostOnIt_IsNot()
        => Assert.False(Signposts.StandOn(new GameScreenData(TileMetrics.TilesAcross, TileMetrics.TilesDown)));

    [Fact]
    public void SignpostedScreen_SerialisedWithAScreen_SurvivesTheRoundTrip()
    {
        // Arrange
        var screen = new GameScreenData(1, 1)
        {
            SignpostedScreen = 499
        };

        // Act
        var restored = MessagePackSerializer.Deserialize<GameScreenData>(
            MessagePackSerializer.Serialize(screen));

        // Assert
        Assert.Equal(499, restored.SignpostedScreen);
    }

    /// <summary>
    ///     Every copy of the map that exists was saved before signposts were part of a screen, so a
    ///     screen holding four values rather than five has to load - with a sign that stands blank
    ///     rather than one that cannot be read at all.
    /// </summary>
    [Fact]
    public void SignpostedScreen_AScreenSavedBeforeSignsWereStored_LoadsWithoutOne()
    {
        // Arrange - the screen as the old format wrote it: tiles, collision map, code, core
        byte[] saved = MessagePackSerializer.ConvertFromJson("[null,[],\"PALMA\",null]");

        // Act
        var restored = MessagePackSerializer.Deserialize<GameScreenData>(saved);

        // Assert
        Assert.Null(restored.SignpostedScreen);
        Assert.Equal("PALMA", restored.TeleportCode);
    }

    /// <summary>
    ///     Two screens: a sign on the first, pointed wherever the test asks, and a teleporter on the
    ///     second with no code until the test gives it one.
    /// </summary>
    private static GameScreenData[] Planet(int? signposted)
    {
        var screens = new GameScreenData[2];

        for (int screen = 0; screen < screens.Length; screen++)
        {
            screens[screen] = new GameScreenData(TileMetrics.TilesAcross, TileMetrics.TilesDown);
        }

        screens[0].ScreenTiles[3, 4].TileImage = (int)ActionTiles.TeleportSignpost;
        screens[0].SignpostedScreen = signposted;

        return screens;
    }
}
