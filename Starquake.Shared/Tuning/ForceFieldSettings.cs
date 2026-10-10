// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;

namespace Starquake.Tuning;

/// <summary>
///     What the force field across a key-locked door does. Driven by <c>Shaders/ForceField.fx</c>;
///     see <c>GameScreenCollection.UpdateKeyLockDoor</c> for where it is stepped and
///     <c>ScreenRenderer.AnimateForceField</c> for where it is handed over.
/// </summary>
/// <remarks>
///     The field is drawn from a single number, how far open the door is, and everything else is read
///     off it: the colour crossfades, the sheet of light draws back onto the jambs to leave the gap
///     clear, and the particle core thins out with it. The number is eased rather than set, so the
///     change reads as something happening rather than something that has happened.
///     <para>
///         A door opening is not the same as a door being passable, and saying it with colour alone
///         is the one difference a player may be unable to see.
///     </para>
///     <para>
///         None of this touches whether Blob can walk through. The cells flip on
///         <c>Collectables.Key</c>, so a door caught halfway through opening is already passable and
///         this is light alone.
///     </para>
/// </remarks>
internal static class ForceFieldSettings
{
    /// <summary>
    ///     What a door burns when it will not let Blob through, and when it will. Hot red and a green
    ///     leaning towards the cyan the rest of the game's energy uses, rather than flat primaries:
    ///     these are added over the room behind them, and a flat green added to anything washes out.
    /// </summary>
    public static Vector3 LockedColour => new(1f, 0.13f, 0.08f);

    /// <inheritdoc cref="LockedColour" />
    public static Vector3 OpenColour => new(0.15f, 1f, 0.4f);

    /// <summary>
    ///     How far the sheet of light is pulled back onto the jambs, shut and open. Under one leaves
    ///     it lit across the whole gap; over one empties the middle, which is what makes an open door
    ///     plainly walkable rather than a green wall.
    /// </summary>
    public const float ShutDrawBack = 0.45f;

    /// <inheritdoc cref="ShutDrawBack" />
    public const float OpenDrawBack = 1.7f;

    /// <summary>
    ///     How hard the field burns, shut and open. Short of full in both cases, because the alpha
    ///     comes from the same number: a field burning at one is opaque, and solid colour across a
    ///     doorway reads as a painted rectangle rather than light holding Blob back. The room has to
    ///     show through. An open door is quieter again, holding nothing back.
    /// </summary>
    public const float ShutIntensity = 0.55f;

    /// <inheritdoc cref="ShutIntensity" />
    public const float OpenIntensity = 0.5f;

    /// <summary>
    ///     How closely the bands travelling up the gap are spaced, in radians over its height. About
    ///     four of them down the door's ninety-six pixels.
    /// </summary>
    public const float Frequency = 26f;

    /// <summary>
    ///     How far those bands travel in a frame. Counted in logic frames rather than drawn ones, so
    ///     the field moves at the same rate whatever the display is doing.
    /// </summary>
    private const float PhasePerFrame = 0.11f;

    /// <summary>
    ///     How long the scanner takes to work its way down the gap, in frames, and how thick the
    ///     bar is as a fraction of the height.
    /// </summary>
    private const int ScanFrames = LogicClock.FramesPerSecond * 7 / 5;

    /// <inheritdoc cref="ScanFrames" />
    private const float ScanWidth = 0.1f;

    /// <summary>
    ///     How hard the jambs burn. They stay lit whatever the sheet between them is doing, so an
    ///     open door still reads as a door rather than a hole in the wall.
    /// </summary>
    public const float PostGlow = 0.55f;

    /// <summary>
    ///     The burst at the moment a door gives way, and how much is left a frame later. The decay is
    ///     a multiply rather than a count, so it is gone in about a fifth of a second without a
    ///     second number saying when.
    /// </summary>
    private const float FlashPeak = 1.6f;

    /// <inheritdoc cref="FlashPeak" />
    private const float FlashLeftPerFrame = 0.8f;

    /// <summary>
    ///     Below this the flash is spent and snaps to nothing, rather than decaying towards it
    ///     forever.
    /// </summary>
    private const float FlashSpent = 0.01f;

    /// <summary>
    ///     How long a door takes to open or shut again, in frames. Short, because it is a door rather
    ///     than a drawbridge, but long enough to be seen happening.
    /// </summary>
    private static readonly int OpeningFrames = LogicClock.Seconds(0.35);

    /// <summary>
    ///     How near Blob has to be for a shut door to brace against him, in pixels, and how much
    ///     harder it burns when he is right up against it.
    /// </summary>
    /// <remarks>
    ///     Measured from Blob's own square rather than the corner the planet is told, which is a
    ///     quarter of a screen's width from the side actually doing the leaning.
    /// </remarks>
    private const int BraceReach = 72;

    /// <inheritdoc cref="BraceReach" />
    private const float BraceBoost = 0.7f;

    /// <summary>
    ///     How far towards open or shut a door is after another frame. Linear, because a door is
    ///     machinery and needs no easing in and out of its own travel.
    /// </summary>
    public static float OpennessAfter(float openness, bool open)
        => MathHelper.Clamp(
            openness + (open ? 1f : -1f) / OpeningFrames, 0f, 1f);

    /// <summary>What is left of the opening flash after another frame.</summary>
    public static float FlashAfter(float flash)
    {
        float left = flash * FlashLeftPerFrame;

        return left < FlashSpent ? 0f : left;
    }

    /// <inheritdoc cref="FlashPeak" />
    public static float NewFlash() => FlashPeak;

    /// <inheritdoc cref="PhasePerFrame" />
    public static float PhaseAt(int logicFrame) => logicFrame * PhasePerFrame;

    /// <summary>How far the scanner has travelled down the gap, as a fraction of its height.</summary>
    public static float ScanAt(int logicFrame)
        => logicFrame % ScanFrames / (float)ScanFrames;

    /// <inheritdoc cref="ScanWidth" />
    public static float OverScanWidth => 1f / ScanWidth;

    /// <summary>
    ///     What the field burns at a given openness: the locked colour crossfaded into the open one.
    /// </summary>
    public static Vector3 ColourAt(float openness)
        => Vector3.Lerp(LockedColour, OpenColour, openness);

    /// <inheritdoc cref="ShutDrawBack" />
    public static float DrawBackAt(float openness)
        => MathHelper.Lerp(ShutDrawBack, OpenDrawBack, openness);

    /// <summary>
    ///     How hard the field burns this frame: the settled brightness for how open it is, the
    ///     flicker shared with the rest of the game's electricity, and the bracing against Blob
    ///     leaning on it. All three are worked out here rather than per pixel.
    /// </summary>
    public static float IntensityAt(float openness, float brace, int logicFrame)
        => MathHelper.Lerp(ShutIntensity, OpenIntensity, openness)
           * ElectricSettings.FlickerAt(logicFrame)
           * (1f + brace * BraceBoost * (1f - openness));

    /// <summary>
    ///     How hard Blob is leaning on the door, from nothing at <see cref="BraceReach" /> away to one
    ///     when he is in the gap. Measured to Blob's square, because that is what the position given
    ///     is a corner of.
    /// </summary>
    /// <remarks>
    ///     A box distance, the furthest of the two axes, rather than a true one. The doorway is a box
    ///     and what is wanted is how far outside it Blob is. It costs no square root, and the
    ///     difference at a corner is not something a brightness shows.
    /// </remarks>
    public static float BraceAgainst(Rectangle doorway, int blobXPos, int blobYPos)
    {
        int outsideX = Math.Max(doorway.Left - (blobXPos + SpriteMetrics.SpriteWidth), blobXPos - doorway.Right);
        int outsideY = Math.Max(doorway.Top - (blobYPos + SpriteMetrics.SpriteHeight), blobYPos - doorway.Bottom);

        int outside = Math.Max(0, Math.Max(outsideX, outsideY));

        return MathHelper.Clamp(1f - outside / (float)BraceReach, 0f, 1f);
    }
}
