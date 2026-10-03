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
///         F8 swaps the artwork, F9 steps the cathode ray tube on a notch, F10 the backdrops,
///         F12 fullscreen, and the player's own pause and quit keys.
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
    private const int TestModeScreenStepDelayFrames = LogicClock.FramesPerSecond / 10;

    /// <summary>
    ///     Frames still to wait before the keypad may step again. Counted down rather than slept
    ///     through: this runs inside <c>GameHost</c>'s fixed-timestep loop, so blocking the thread
    ///     here stops the whole game - draw and sound included - and then leaves the loop a backlog
    ///     to fast-forward through.
    /// </summary>
    private int _framesUntilNextStep;

    public void Update()
    {
        if (input.KeyboardState.IsKeyDown(Keys.F1))
        {
            if (player.BlobsState == BlobState.Normal)
            {
                player.KillBlob(false, false);
            }
        }

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

    /// <summary>
    ///     The keypad walks the map: NumPad4 and NumPad6 step a screen left and right, NumPad8 and
    ///     NumPad2 a row up and down. Held keys combine, so 4 and 8 together step diagonally.
    /// </summary>
    /// <remarks>
    ///     The keys are resolved into one destination and applied once. Applying them one at a time
    ///     walked the map through an intermediate screen, and arriving somewhere is not free here -
    ///     <c>PerformPostScreenChangeActions</c> marks a screen visited and scores it, and hands over
    ///     to the core screen if that is where it has landed. Stepping left and up from 200 passed
    ///     through 199 to reach 183, so it scored a screen nobody had been on and handed over to the
    ///     core on the way past.
    ///     <para>
    ///         Working in rows and columns rather than in the flat screen index is what keeps a step
    ///         on an edge from wrapping onto the row above or below. The index alone cannot tell the
    ///         difference: screen 16 is the first of row 1, and one to the left of it is 15, which is
    ///         the last of row 0 rather than anywhere next to it.
    ///     </para>
    /// </remarks>
    private void StepBetweenScreens()
    {
        if (_framesUntilNextStep > 0)
        {
            _framesUntilNextStep--;

            return;
        }

        int column = gameScreens.ScreenNumber % WorldMap.ScreensAcross;
        int row = gameScreens.ScreenNumber / WorldMap.ScreensAcross;

        if (input.KeyboardState.IsKeyDown(Keys.NumPad4) && column > 0)
        {
            column--;
        }

        if (input.KeyboardState.IsKeyDown(Keys.NumPad6) && column < WorldMap.ScreensAcross - 1)
        {
            column++;
        }

        if (input.KeyboardState.IsKeyDown(Keys.NumPad8) && row > 0)
        {
            row--;
        }

        if (input.KeyboardState.IsKeyDown(Keys.NumPad2) && row < WorldMap.ScreensDown - 1)
        {
            row++;
        }

        int destination = row * WorldMap.ScreensAcross + column;

        if (destination == gameScreens.ScreenNumber)
        {
            return;
        }

        gameScreens.ScreenNumber = destination;
        screenDirectory.MainGame.PerformPostScreenChangeActions(GameScreenType.MainGame, new ScreenChange.KeepPicture());
        _framesUntilNextStep = TestModeScreenStepDelayFrames;
    }
}
