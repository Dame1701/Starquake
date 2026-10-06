// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Core;
using Starquake.GameComponents;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     <see cref="ScreenDirectory" /> was a dictionary, a <see cref="GameScreenType" /> field and a
///     lookup method on <c>GameEngine</c>, so none of it could be exercised without a graphics
///     device and a loaded content pipeline behind it.
///     <para>
///         What is worth pinning is the difference between its two jobs, because the engine relies
///         on it. Asking for the <em>current</em> screen returns null, because update and draw run
///         every frame whatever <see cref="ScreenDirectory.Current" /> has been set to and have
///         always skipped a screen they could not find. Asking for a screen <em>by name</em> -
///         now a typed property per screen - throws, since a screen handing over to another has no
///         way to carry on without it.
///     </para>
///     <para>
///         Only the throwing half of the named lookups can be pinned here: the ten typed properties
///         return real screen components, and one of those cannot be constructed without a graphics
///         device. What they would otherwise have needed - an <see cref="IGameScreen" /> wide
///         enough for a stub to stand in for any screen - is the thing that put eleven empty and
///         throwing members on the screens in the first place.
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
    public void ANamedScreen_ThatWasNeverRegistered_ThrowsNamingIt()
    {
        // Arrange
        // Every screen the directory hands back by name, and the property that does it. A screen
        // missing from here is one no other screen can hand over to. These stay in one test rather
        // than a theory because both the enum and the directory are internal, and a theory's
        // parameters would have to be public.
        var directory = new ScreenDirectory();
        (GameScreenType Screen, Func<object> Lookup)[] namedLookups =
        [
            (GameScreenType.AccessBooth, () => directory.AccessBooth),
            (GameScreenType.Cheops, () => directory.Cheops),
            (GameScreenType.ComputerReport, () => directory.ComputerReport),
            (GameScreenType.GameComplete, () => directory.GameComplete),
            (GameScreenType.GameOver, () => directory.GameOver),
            (GameScreenType.HiScores, () => directory.HiScores),
            (GameScreenType.MainGame, () => directory.MainGame),
            (GameScreenType.Options, () => directory.Options),
            (GameScreenType.PlanetCore, () => directory.PlanetCore),
            (GameScreenType.Teleporter, () => directory.Teleporter)
        ];

        foreach ((GameScreenType screen, Func<object> lookup) in namedLookups)
        {
            // Act
            var thrown = Assert.Throws<InvalidOperationException>(() => lookup());

            // Assert
            Assert.Contains(screen.ToString(), thrown.Message);
        }
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

    /// <summary>
    ///     Stands in for a screen's component in the frame loop. Stepping it and drawing it is the
    ///     whole of <see cref="IGameScreen" />, and the directory only ever stores these and hands
    ///     them back, so neither needs to do anything.
    /// </summary>
    private sealed class StubScreen : IGameScreen
    {
        public void Update(GameTime gameTime)
        {
        }

        public void Draw()
        {
        }
    }
}
