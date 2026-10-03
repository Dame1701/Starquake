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
internal class TestModeKeys(
    ToggleKeys input,
    Blob player,
    PlayerStats stats,
    ScreenEffectEngine screenEffects,
    ScreenDirectory screenDirectory,
    GameScreenCollection gameScreens)
{
    /// <summary>
    ///     Test-mode cheats: how many lives F2 hands back, and how long the screen-stepping keys wait
    ///     between screens so that holding one down steps rather than races.
    /// </summary>
    private const int TestModeLivesRestored = 5;

    /// <inheritdoc cref="TestModeLivesRestored" />
    private const int TestModeScreenStepDelayMs = 100;

    public void Update()
    {
        if (input.KeyboardState.IsKeyDown(Keys.F1))
        {
            if (player.BlobsState == BlobState.Normal)
            {
                player.KillBlob(false, false);
            }
        }

        // Give us some lives back (Testing)
        if (input.KeyboardState.IsKeyDown(Keys.F2))
        {
            stats.BlobLives = TestModeLivesRestored;
            player.BlobsState = BlobState.Dead;
        }

        if (input.GetPressedKey() == Keys.F3)
        {
            screenEffects.ExecuteScreenEffect(ScreenEffectType.AdditiveFade, -1, 50, false);
        }

        if (screenDirectory.Current == GameScreenType.MainGame)
        {
            StepBetweenScreens();
        }
    }

    private void StepBetweenScreens()
    {
        if (input.KeyboardState.IsKeyDown(Keys.NumPad4))
        {
            if (gameScreens.ScreenNumber > 0)
            {
                gameScreens.ScreenNumber--;
                screenDirectory.MainGame.PerformPostScreenChangeActions(GameScreenType.MainGame, new ScreenChange.KeepPicture());
                Thread.Sleep(TestModeScreenStepDelayMs);
            }
        }

        if (input.KeyboardState.IsKeyDown(Keys.NumPad6))
        {
            if (gameScreens.ScreenNumber < WorldMap.LastScreenNumber)
            {
                gameScreens.ScreenNumber++;
                screenDirectory.MainGame.PerformPostScreenChangeActions(GameScreenType.MainGame, new ScreenChange.KeepPicture());
                Thread.Sleep(TestModeScreenStepDelayMs);
            }
        }

        if (input.KeyboardState.IsKeyDown(Keys.NumPad8))
        {
            if (gameScreens.ScreenNumber - WorldMap.ScreensAcross >= 0)
            {
                gameScreens.ScreenNumber -= WorldMap.ScreensAcross;
                screenDirectory.MainGame.PerformPostScreenChangeActions(GameScreenType.MainGame, new ScreenChange.KeepPicture());
                Thread.Sleep(TestModeScreenStepDelayMs);
            }
        }

        if (!input.KeyboardState.IsKeyDown(Keys.NumPad2))
        {
            return;
        }

        if (gameScreens.ScreenNumber + WorldMap.ScreensAcross > WorldMap.LastScreenNumber)
        {
            return;
        }

        gameScreens.ScreenNumber += WorldMap.ScreensAcross;
        screenDirectory.MainGame.PerformPostScreenChangeActions(GameScreenType.MainGame, new ScreenChange.KeepPicture());
        Thread.Sleep(TestModeScreenStepDelayMs);
    }
}
