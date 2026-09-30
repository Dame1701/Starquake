// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Input;
using Starquake.Input;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     <see cref="GameSession" /> holds the pause and the key bindings, which were two properties on
///     <c>GameEngine</c> whose setters reached out to the sound engine and to the blob. Neither could
///     be exercised without standing up the whole engine, so neither ever was - and one of them had
///     a bug in it for the life of the repo.
///     <para>
///         What these pin is that setting either one announces it, because the engine's reaction is
///         hung off that announcement: the background rumble follows the pause, and the blob's
///         control follows the key set. A set that stopped raising would fail silently in play.
///     </para>
/// </summary>
public class GameSessionTests
{
    [Fact]
    public void Paused_OnANewSession_IsFalseSoPlayStartsRunning()
    {
        // Act
        var session = new GameSession();

        // Assert
        Assert.False(session.Paused);
    }

    [Fact]
    public void Paused_WhenSet_AnnouncesTheValueItWasSetTo()
    {
        // Arrange
        var session = new GameSession();
        var announced = new List<bool>();
        session.OnPausedChanged += paused => announced.Add(paused);

        // Act
        session.Paused = true;
        session.Paused = false;

        // Assert
        // The engine stops the background rumble on the first and restarts it on the second.
        Assert.Equal(new[]
        {
            true,
            false
        }, announced);
    }

    [Fact]
    public void Paused_SetToWhatItAlreadyWas_StillAnnounces()
    {
        // Arrange
        var session = new GameSession();
        int announcements = 0;
        session.OnPausedChanged += _ => announcements++;

        // Act
        session.Paused = false;

        // Assert
        // Pinning what it does rather than what it might: the old setter did not compare either,
        // and the sound engine's enable and disable are both safe to call twice.
        Assert.Equal(1, announcements);
    }

    [Fact]
    public void PlayerKeys_OnANewSession_IsTheCursorKeysWithLeftControlToFire()
    {
        // Act
        var session = new GameSession();

        // Assert
        Assert.Equal(Keys.Up, session.PlayerKeys.Up);
        Assert.Equal(Keys.Down, session.PlayerKeys.Down);
        Assert.Equal(Keys.Left, session.PlayerKeys.Left);
        Assert.Equal(Keys.Right, session.PlayerKeys.Right);
        Assert.Equal(Keys.LeftControl, session.PlayerKeys.Fire);
    }

    [Fact]
    public void PlayerKeys_WhenSet_AnnouncesTheNewSet()
    {
        // Arrange
        var session = new GameSession();
        PlayerKeySet? announced = null;
        session.OnPlayerKeysChanged += keys => announced = keys;
        var spectrumLayout = new PlayerKeySet
        {
            Up = Keys.Q,
            Down = Keys.A,
            Left = Keys.O,
            Right = Keys.P,
            Fire = Keys.Space
        };

        // Act
        session.PlayerKeys = spectrumLayout;

        // Assert
        // The engine answers this by handing the set to the blob's control.
        Assert.Same(spectrumLayout, announced);
    }

    [Fact]
    public void PlayerKeys_WhenSet_IsWhatIsReadBackAfterwards()
    {
        // Arrange
        // The blob is built after the options screen can be used, and reads the set off the session
        // when it is built, so what is stored has to be the set that was last assigned.
        var session = new GameSession();
        var joystick = new PlayerKeySet
        {
            Up = Keys.None,
            Down = Keys.None,
            Left = Keys.None,
            Right = Keys.None,
            Fire = Keys.None
        };

        // Act
        session.PlayerKeys = joystick;

        // Assert
        Assert.Same(joystick, session.PlayerKeys);
    }

    [Fact]
    public void PlayerKeys_TheJoystickLayout_LeavesEveryMovementKeyUnbound()
    {
        // Arrange
        // This is the shape the engine tests for when it decides to use the joypad. All five have
        // to be None - the check read Left twice and never Fire until it was corrected.
        var joystick = new PlayerKeySet
        {
            Up = Keys.None,
            Down = Keys.None,
            Left = Keys.None,
            Right = Keys.None,
            Fire = Keys.None
        };

        // Act
        var session = new GameSession
        {
            PlayerKeys = joystick
        };

        // Assert
        Assert.Equal(Keys.None, session.PlayerKeys.Up);
        Assert.Equal(Keys.None, session.PlayerKeys.Down);
        Assert.Equal(Keys.None, session.PlayerKeys.Left);
        Assert.Equal(Keys.None, session.PlayerKeys.Right);
        Assert.Equal(Keys.None, session.PlayerKeys.Fire);
    }

    [Fact]
    public void OnPausedChanged_WithNothingListening_DoesNotThrow()
    {
        // Arrange
        // The session is built before anything subscribes, and the options screen can move the
        // pause before the engine has wired itself up.
        var session = new GameSession();

        // Act, Assert
        session.Paused = true;
        Assert.True(session.Paused);
    }
}
