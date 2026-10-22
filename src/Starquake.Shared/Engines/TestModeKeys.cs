// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework.Input;
using Starquake.ActiveSprites;
using Starquake.Constants;
using Starquake.Core;
using Starquake.GameComponents;
using Starquake.Input;
using Starquake.Scoring;
using Starquake.World;

namespace Starquake.Engines;

/// <summary>
///     The debug keys: F1 kills Blob, F2 refills his lives, F4 hands him a core
///     element the core is still missing, F7 hands him the key and the access card, and the
///     numeric keypad steps between screens.
/// </summary>
/// <remarks>
///     Nothing in a release build constructs or calls this. <c>GameEngine</c> builds and steps it
///     from two <see cref="System.Diagnostics.ConditionalAttribute" /> methods, which removes the
///     calls rather than leaving a branch never taken, so a release has no path to any of it.
///     <para>
///         The keys that are not debug keys belong to <see cref="GlobalHotkeys" /> and stay live in
///         both builds: F5 takes a screenshot, F8 swaps the artwork, F9 steps the cathode ray tube,
///         F10 the backdrops, F11 the speed, F12 fullscreen, plus the player's own pause and quit keys.
///     </para>
/// </remarks>
internal class TestModeKeys(
    ToggleKeys input,
    Blob player,
    PlayerStats stats,
    ScreenDirectory screenDirectory,
    GameScreenCollection gameScreens,
    CoreElements coreElements)
{
    /// <summary>
    ///     How many lives F2 hands back, and how long the screen-stepping keys wait between screens so
    ///     holding one down steps rather than races.
    /// </summary>
    private const int TestModeLivesRestored = 5;

    /// <inheritdoc cref="TestModeLivesRestored" />
    private const int TestModeScreenStepDelayFrames = LogicClock.FramesPerSecond / 10;

    /// <summary>
    ///     Frames still to wait before the keypad may step again. Counted down rather than slept
    ///     through: this runs inside <c>GameHost</c>'s fixed-timestep loop, so blocking the thread
    ///     stops the whole game, draw and sound included, and leaves the loop a backlog to
    ///     fast-forward through.
    /// </summary>
    private int _framesUntilNextStep;

    public void Update()
    {
        if (input.KeyboardState.IsKeyDown(Keys.F1))
        {
            if (player.State == BlobState.Normal)
            {
                player.Kill(false, false);
            }
        }

        if (input.KeyboardState.IsKeyDown(Keys.F2))
        {
            stats.BlobLives = TestModeLivesRestored;
            player.State = BlobState.Dead;
        }

        if (input.GetPressedKey() == Keys.F4)
        {
            HandOverACoreElement();
        }

        // F7 rather than the F5 that would follow F4. macOS takes F5 and F6 for itself and the game
        // never sees them, even with the function keys set to be function keys and no shortcut of the
        // user's own on either. F7 arrives, as F8 to F12 do.
        if (input.GetPressedKey() == Keys.F7)
        {
            HandOverTheWayIn();
        }

        if (screenDirectory.Current == GameScreenType.MainGame)
        {
            StepBetweenScreens();
        }
    }

    /// <summary>
    ///     Puts one core element the core is still missing into an empty inventory slot, so the
    ///     replacement sequence can be reached without finding nine of them on the planet. Press four
    ///     times to fill Blob up, then walk into the core.
    /// </summary>
    /// <remarks>
    ///     Never an element Blob already carries. The core matches every inventory slot against every
    ///     element it wants, so two copies of one would queue that element twice, scoring it twice and
    ///     starting its replacement again while the first was still running.
    /// </remarks>
    private void HandOverACoreElement()
    {
        if (FirstEmptySlot(player.Carrying) is not { } slot)
        {
            return;
        }

        if (WantedElementNotYetCarried(coreElements.ElementsWanted(), player.Carrying) is not { } element)
        {
            return;
        }

        player.Carrying[slot] = (Collectables)element;
    }

    /// <summary>
    ///     Puts the key and the access card into Blob's hands, so the two things the planet locks, the
    ///     force field across a key door and the booth that asks for a code, can be reached without
    ///     hunting down the one of each the world holds.
    /// </summary>
    /// <remarks>
    ///     Either may already be there. The pair is handed over one at a time and each is skipped if
    ///     Blob has it, so pressing twice does not fill two more slots with a second key and card. A
    ///     slot spent on a duplicate is a slot that cannot carry an element.
    /// </remarks>
    private void HandOverTheWayIn()
    {
        HandOver(Collectables.Key);
        HandOver(Collectables.AccessCard);
    }

    /// <inheritdoc cref="HandOverTheWayIn" />
    private void HandOver(Collectables item)
    {
        if (SlotFor(player.Carrying, item) is { } slot)
        {
            player.Carrying[slot] = item;
        }
    }

    /// <summary>
    ///     Where an item the key hands over should go, or null when it should not be handed over at
    ///     all, either because Blob already carries one or because he has no room.
    /// </summary>
    public static int? SlotFor(Collectables[] carried, Collectables item)
        => carried.Contains(item) ? null : FirstEmptySlot(carried);

    /// <summary>The first empty inventory slot, or null when Blob is full.</summary>
    public static int? FirstEmptySlot(Collectables[] carried)
    {
        for (int slot = 0; slot < carried.Length; slot++)
        {
            if (carried[slot] == Collectables.None)
            {
                return slot;
            }
        }

        return null;
    }

    /// <summary>
    ///     An element the core is still missing that Blob does not already carry, or null when every
    ///     one it wants is either in his hands or already in it.
    /// </summary>
    /// <remarks>
    ///     Takes the element numbers rather than the <c>CoreElements</c> they came off, so the rule
    ///     can be checked without a content pipeline: building a real set needs a resource manager,
    ///     and that needs the game's content.
    /// </remarks>
    public static int? WantedElementNotYetCarried(IEnumerable<int> wanted, Collectables[] carried)
    {
        foreach (int element in wanted)
        {
            if (!carried.Contains((Collectables)element))
            {
                return element;
            }
        }

        return null;
    }

    /// <summary>
    ///     The keypad walks the map: NumPad4 and NumPad6 step a screen left and right, NumPad8 and
    ///     NumPad2 a row up and down. Held keys combine, so 4 and 8 together step diagonally.
    /// </summary>
    /// <remarks>
    ///     The keys are resolved into one destination and applied once. Arriving somewhere is not
    ///     free: <c>PerformPostScreenChangeActions</c> marks a screen visited, scores it, and hands
    ///     over to the core screen if that is where it landed. Applying the keys one at a time walks
    ///     through an intermediate screen, so stepping left and up from 200 passes through 199 and
    ///     scores a screen nobody visited while handing over to the core on the way past.
    ///     <para>
    ///         Working in rows and columns rather than the flat screen index is what stops a step on
    ///         an edge wrapping onto the row above or below. The index alone cannot tell: screen 16 is
    ///         the first of row 1, and one to its left is 15, the last of row 0 rather than anywhere
    ///         next to it.
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
