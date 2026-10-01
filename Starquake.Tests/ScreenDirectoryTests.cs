// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.GameComponents;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     <see cref="ScreenDirectory" /> was a dictionary, a <see cref="GameScreenType" /> field and a
///     lookup method on <c>GameEngine</c>, so none of it could be exercised without a graphics
///     device and a loaded content pipeline behind it.
///     <para>
///         What is worth pinning is the difference between the two lookups, because the engine
///         relies on it: asking for a screen <em>by name</em> throws when it is not there, since a
///         screen asking for another has no way to carry on without it, while asking for the
///         <em>current</em> screen returns null, because update and draw run every frame whatever
///         <see cref="ScreenDirectory.Current" /> has been set to and have always skipped a screen
///         they could not find.
///     </para>
/// </summary>
public class ScreenDirectoryTests
{
    [Fact]
    public void Current_OnANewDirectory_IsTheOptionsScreenTheGameBootsInto()
    {
        // Act
        var directory = new ScreenDirectory();

        // Assert
        Assert.Equal(GameScreenType.Options, directory.Current);
    }

    [Fact]
    public void Get_AScreenThatWasRegistered_ReturnsIt()
    {
        // Arrange
        var directory = new ScreenDirectory();
        var teleporter = new StubScreen();
        directory.Add(GameScreenType.Teleporter, teleporter);

        // Act
        IGameScreen found = directory.Get(GameScreenType.Teleporter);

        // Assert
        Assert.Same(teleporter, found);
    }

    [Fact]
    public void Get_AScreenThatWasNeverRegistered_ThrowsNamingIt()
    {
        // Arrange
        var directory = new ScreenDirectory();

        // Act
        var thrown = Assert.Throws<InvalidOperationException>(
            () => directory.Get(GameScreenType.Cheops));

        // Assert
        Assert.Contains("Cheops", thrown.Message);
    }

    [Fact]
    public void Add_TheSameScreenTwice_Throws()
    {
        // Arrange
        var directory = new ScreenDirectory();
        directory.Add(GameScreenType.MainGame, new StubScreen());

        // Act, Assert
        // Two components claiming one screen means one would never be drawn, and nothing else
        // would report it.
        Assert.ThrowsAny<ArgumentException>(
            () => directory.Add(GameScreenType.MainGame, new StubScreen()));
    }

    [Fact]
    public void CurrentScreen_WhenTheCurrentScreenIsRegistered_IsThatScreensComponent()
    {
        // Arrange
        var directory = new ScreenDirectory();
        var mainGame = new StubScreen();
        directory.Add(GameScreenType.MainGame, mainGame);
        directory.Current = GameScreenType.MainGame;

        // Act
        IGameScreen? current = directory.CurrentScreen;

        // Assert
        Assert.Same(mainGame, current);
    }

    [Fact]
    public void CurrentScreen_WhenTheCurrentScreenHasNoComponent_IsNullRatherThanThrowing()
    {
        // Arrange
        var directory = new ScreenDirectory();
        directory.Add(GameScreenType.MainGame, new StubScreen());
        directory.Current = GameScreenType.None;

        // Act
        IGameScreen? current = directory.CurrentScreen;

        // Assert
        // The engine's update and draw dereference this conditionally every frame.
        Assert.Null(current);
    }

    [Fact]
    public void Current_SetToAnotherScreen_MovesCurrentScreenWithIt()
    {
        // Arrange
        var directory = new ScreenDirectory();
        var options = new StubScreen();
        var hiScores = new StubScreen();
        directory.Add(GameScreenType.Options, options);
        directory.Add(GameScreenType.HiScores, hiScores);

        // Act
        // This is all switching screen is: twenty-four places assign to Current.
        directory.Current = GameScreenType.HiScores;

        // Assert
        Assert.Same(hiScores, directory.CurrentScreen);
    }

    [Fact]
    public void All_AfterRegistering_HoldsEveryScreenInTheOrderTheyWereAdded()
    {
        // Arrange
        var directory = new ScreenDirectory();
        var options = new StubScreen();
        var mainGame = new StubScreen();
        var cheops = new StubScreen();
        directory.Add(GameScreenType.Options, options);
        directory.Add(GameScreenType.MainGame, mainGame);
        directory.Add(GameScreenType.Cheops, cheops);

        // Act
        IEnumerable<IGameScreen> all = directory.All;

        // Assert
        // The engine loads content by walking this, so a screen missing here never loads.
        Assert.Equal(new IGameScreen[]
        {
            options,
            mainGame,
            cheops
        }, all);
    }

    /// <summary>
    ///     Stands in for a screen's component. The directory only ever stores and hands these back,
    ///     so nothing here needs to do anything.
    /// </summary>
    private sealed class StubScreen : IGameScreen
    {
        public int ScreenFade { get; set; }

        public bool Success => false;

        public void Update(GameTime gameTime)
        {
        }

        public void Reset(object? data)
        {
        }

        public void Draw(GameTime gameTime)
        {
        }

        public void PerformPostScreenChangeActions(GameScreenType triggeringScreen, ScreenChange change)
        {
        }
    }
}
