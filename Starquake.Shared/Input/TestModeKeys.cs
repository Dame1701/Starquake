// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Input;
using Starquake.ActiveSprites;
using Starquake.Constants;
using Starquake.Display;
using Starquake.Engines.ScreenEffects;
using Starquake.GameComponents;
using Starquake.Scoring;
using Starquake.Structure;

namespace Starquake.Input;

/// <summary>
///     The debug keys: F1 kills Blob, F2 refills his lives, F3 runs the fade effect, and the
///     numeric keypad steps between screens.
/// </summary>
/// <remarks>
///     Nothing in a release build constructs this or calls it. <c>GameEngine</c> builds it and steps
///     it from two <see cref="System.Diagnostics.ConditionalAttribute" /> methods, which removes the
///     calls rather than leaving a branch that is never taken, so a release has no path to any of
///     this at all.
///     <para>
///         The keys that are not debug keys are <see cref="GlobalHotkeys" />' and stay live in both:
///         F8 swaps the artwork, F9 the scanlines, F10 the backdrops, F12 fullscreen, and the
///         player's own pause and quit keys.
///     </para>
/// </remarks>
internal class TestModeKeys
{
    /// <summary>
    ///     Test-mode cheats: how many lives F2 hands back, and how long the screen-stepping keys wait
    ///     between screens so that holding one down steps rather than races.
    /// </summary>
    private const int TestModeLivesRestored = 5;

    /// <inheritdoc cref="TestModeLivesRestored" />
    private const int TestModeScreenStepDelayMs = 100;

    private readonly GameScreenCollection _gameScreens;
    private readonly ToggleKeys _input;
    private readonly Blob _player;
    private readonly ScreenDirectory _screenDirectory;
    private readonly ScreenEffectEngine _screenEffects;
    private readonly PlayerStats _stats;

    public TestModeKeys(
        ToggleKeys input,
        Blob player,
        PlayerStats stats,
        ScreenEffectEngine screenEffects,
        ScreenDirectory screenDirectory,
        GameScreenCollection gameScreens)
    {
        _input = input;
        _player = player;
        _stats = stats;
        _screenEffects = screenEffects;
        _screenDirectory = screenDirectory;
        _gameScreens = gameScreens;
    }

    public void Update()
    {
        if (_input.KeyboardState.IsKeyDown(Keys.F1))
        {
            if (_player.BlobsState == BlobState.Normal)
            {
                _player.KillBlob(false, false);
            }
        }

        // Give us some lives back (Testing)
        if (_input.KeyboardState.IsKeyDown(Keys.F2))
        {
            _stats.BlobLives = TestModeLivesRestored;
            _player.BlobsState = BlobState.Dead;
        }

        if (_input.GetPressedKey() == Keys.F3)
        {
            _screenEffects.ExecuteScreenEffect(ScreenEffectType.AdditiveFade, -1, 50, false);
        }

        if (_screenDirectory.Current == GameScreenType.MainGame)
        {
            StepBetweenScreens();
        }
    }

    private void StepBetweenScreens()
    {
        if (_input.KeyboardState.IsKeyDown(Keys.NumPad4))
        {
            if (_gameScreens.ScreenNumber > 0)
            {
                _gameScreens.ScreenNumber--;
                _screenDirectory.MainGame.PerformPostScreenChangeActions(GameScreenType.MainGame, new ScreenChange.KeepPicture());
                Thread.Sleep(TestModeScreenStepDelayMs);
            }
        }

        if (_input.KeyboardState.IsKeyDown(Keys.NumPad6))
        {
            if (_gameScreens.ScreenNumber < WorldMap.LastScreenNumber)
            {
                _gameScreens.ScreenNumber++;
                _screenDirectory.MainGame.PerformPostScreenChangeActions(GameScreenType.MainGame, new ScreenChange.KeepPicture());
                Thread.Sleep(TestModeScreenStepDelayMs);
            }
        }

        if (_input.KeyboardState.IsKeyDown(Keys.NumPad8))
        {
            if (_gameScreens.ScreenNumber - WorldMap.ScreensAcross >= 0)
            {
                _gameScreens.ScreenNumber -= WorldMap.ScreensAcross;
                _screenDirectory.MainGame.PerformPostScreenChangeActions(GameScreenType.MainGame, new ScreenChange.KeepPicture());
                Thread.Sleep(TestModeScreenStepDelayMs);
            }
        }

        if (_input.KeyboardState.IsKeyDown(Keys.NumPad2))
        {
            if (_gameScreens.ScreenNumber + WorldMap.ScreensAcross <= WorldMap.LastScreenNumber)
            {
                _gameScreens.ScreenNumber += WorldMap.ScreensAcross;
                _screenDirectory.MainGame.PerformPostScreenChangeActions(GameScreenType.MainGame, new ScreenChange.KeepPicture());
                Thread.Sleep(TestModeScreenStepDelayMs);
            }
        }
    }
}
