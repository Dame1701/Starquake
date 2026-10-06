// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.Tuning;

/// <summary>
///     What Blob's shot burns like. Driven by <c>Shaders/Laser.fx</c>; see <c>BlobWeapon</c>.
/// </summary>
/// <remarks>
///     There is no artwork behind it. The beam is laid over a plain filled rectangle turned to
///     the way the shot is going, so every pixel of it is worked out: a hot line down the middle,
///     colour spreading off it, both ends taken out, and the whole thing snaking.
///     <para>
///         Two things were tried before this and both were the same mistake. A flame sprite was
///         drawn on foot - the one that had been taken out of the game years ago for adding
///         nothing - and then a white glow was laid over that. What the shot wanted was not a
///         picture of a flame with light on it, but light of its own.
///     </para>
/// </remarks>
internal static class LaserSettings
{
    /// <summary>
    ///     How tight the white line down the middle is, as a fraction of the half-width. Small, or
    ///     it is a band rather than a beam.
    /// </summary>
    public const float CoreWidth = 0.3f;

    /// <summary>What burns down the middle of the shot.</summary>
    public static Vector3 CoreColour => new(1f, 0.97f, 0.9f);

    /// <summary>
    ///     What spreads off it. The cyan the redrawn flame is already drawn in, so the spread is
    ///     more of the same light rather than a second colour.
    /// </summary>
    public static Vector3 EdgeColour => new(0.3f, 0.8f, 0.95f);

    /// <summary>How quickly the coloured spread falls away from the axis.</summary>
    public const float Spread = 1.35f;

    /// <summary>
    ///     How long the beam is and how thick, in pixels, for a shot fired from a shuttle pad. The
    ///     shot itself is two by thirty-two, so the beam is a little longer than the thing it is
    ///     the light of and no more - it was half again at first, which read as a bar flying along
    ///     beside him.
    /// </summary>
    public const int Length = 40;

    /// <inheritdoc cref="Length" />
    public const int Thickness = 12;

    /// <summary>
    ///     What a shot fired on foot gets of each of those, and of the intensity.
    /// </summary>
    /// <remarks>
    ///     Blob off the pad is not carrying the pad's power, and the fire should say so. It was the
    ///     same beam either way, which made walking about look as though he had a shuttle's weapon
    ///     in his hands.
    /// </remarks>
    private const float OnFootLength = 0.6f;

    /// <inheritdoc cref="OnFootLength" />
    private const float OnFootThickness = 0.62f;

    /// <inheritdoc cref="OnFootLength" />
    private const float OnFootIntensity = 0.5f;

    /// <summary>
    ///     How far the beam snakes either side of true, as a fraction of its half-thickness, and
    ///     how many waves run along it.
    /// </summary>
    public const float WaveAmplitude = 0.2f;

    /// <inheritdoc cref="WaveAmplitude" />
    public const float WaveFrequency = 9f;

    /// <summary>How fast the waves travel, in radians per logic frame.</summary>
    private const double WaveSpeed = 0.9;

    /// <summary>
    ///     How much of the back is taken out, as a fraction of the length, so the beam trails away
    ///     rather than ending in a cut edge. The front is not faded: that is where the shot is,
    ///     and it carries the head.
    /// </summary>
    public const float EndFade = 0.42f;

    /// <summary>
    ///     The balls of plasma at either end of the shot. There used to be an animated sprite at
    ///     the front and there is not now - both ends are light, like the rest of it.
    /// </summary>
    /// <remarks>
    ///     <see cref="HeadStretch" /> squashes them along the beam, so they are balls drawn out
    ///     into the shot rather than beads stuck on its ends. Below one they would be drawn out so
    ///     far as to be a second beam.
    /// </remarks>
    public const float HeadSize = 0.85f;

    /// <inheritdoc cref="HeadSize" />
    public const float HeadStretch = 2.6f;

    /// <inheritdoc cref="HeadSize" />
    public const float HeadBoost = 1.35f;

    /// <summary>
    ///     How much of that the trailing ball gets. The shot is at the front and the back is only
    ///     its wake, so the two are not the same: a matched pair reads as a dumbbell flying
    ///     sideways rather than as something travelling.
    /// </summary>
    public const float BackShare = 0.62f;

    /// <summary>
    ///     Where the waves have travelled to by a given logic frame. A logic frame, so the beam
    ///     writhes at the same rate whatever the display is doing.
    /// </summary>
    public static float WavePhaseAt(int logicFrame) => (float)(logicFrame * WaveSpeed);

    /// <summary>
    ///     How long, how thick and how hard a shot burns, which depends on whether Blob is firing
    ///     from a shuttle pad or out of his own hands.
    /// </summary>
    public static (int Length, int Thickness, float Intensity) BeamFor(bool onShuttle)
        => onShuttle
            ? (Length, Thickness, Intensity)
            : ((int)(Length * OnFootLength),
                (int)(Thickness * OnFootThickness),
                Intensity * OnFootIntensity);

    /// <summary>
    ///     How hard the whole thing burns. The one knob for "too much" - the white glow this
    ///     replaced had three between them.
    /// </summary>
    /// <remarks>
    ///     Three goes at this were spent turning the beam down when what was washing the screen out
    ///     was the trail behind it: <c>FireTrail</c> balloons every particle to 256 pixels, and it
    ///     had been given three times as many of them living twice as long. With the trail back as
    ///     it was, the beam turned out to have been close to right all along and wants more light
    ///     rather than less - it is above where it stood when it was called too intense.
    ///     <para>
    ///         It has since gone well past the value it started at, which is worth saying plainly:
    ///         a number that was called too intense and is now settled at half as much again looks
    ///         like a mistake to anybody reading it cold. It is not. Over one is deliberate too -
    ///         the beam is drawn additively, so this is how much light it adds rather than an
    ///         opacity, and there is nothing to clip against short of white. So if the shot is
    ///         ever too much again, look at what else is being drawn on it before turning this
    ///         down.
    ///     </para>
    /// </remarks>
    public const float Intensity = 1.15f;
}
