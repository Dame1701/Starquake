// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;

namespace Starquake.Tuning;

/// <summary>
///     What Blob's shot burns like. Driven by <c>Shaders/Laser.fx</c>; see <c>BlobWeapon</c>.
/// </summary>
/// <remarks>
///     No artwork behind it. The beam is laid over a plain filled rectangle turned to the direction
///     of travel, so every pixel is worked out: a hot line down the middle, colour spreading off it,
///     both ends taken out, and the whole thing snaking. The shot wants light of its own rather than
///     a picture of a flame with light on it.
/// </remarks>
internal static class LaserSettings
{
    /// <summary>
    ///     How tight the white line down the middle is, as a fraction of the half-width. Small, or it
    ///     reads as a band rather than a beam.
    /// </summary>
    public const float CoreWidth = 0.3f;

    /// <summary>How quickly the coloured spread falls away from the axis.</summary>
    public const float Spread = 1.35f;

    /// <summary>
    ///     How long and thick the beam is, in pixels, for a shot fired from a shuttle pad. The shot
    ///     itself is two by thirty-two, so the beam is only a little longer than the thing it is the
    ///     light of. Much longer and it reads as a bar flying alongside Blob.
    /// </summary>
    public const int Length = 40;

    /// <inheritdoc cref="Length" />
    public const int Thickness = 12;

    /// <summary>What a shot fired on foot gets of each of those, and of the intensity.</summary>
    /// <remarks>
    ///     Blob off the pad is not carrying the pad's power and the fire should say so. The same beam
    ///     either way makes walking about look as though he has a shuttle's weapon in his hands.
    /// </remarks>
    private const float OnFootLength = 0.6f;

    /// <inheritdoc cref="OnFootLength" />
    private const float OnFootThickness = 0.62f;

    /// <inheritdoc cref="OnFootLength" />
    private const float OnFootIntensity = 0.5f;

    /// <summary>
    ///     How far the beam snakes either side of true, as a fraction of its half-thickness, and how
    ///     many waves run along it.
    /// </summary>
    public const float WaveAmplitude = 0.2f;

    /// <inheritdoc cref="WaveAmplitude" />
    public const float WaveFrequency = 9f;

    /// <summary>How fast the waves travel, in radians per logic frame.</summary>
    private const double WaveSpeed = 0.9;

    /// <summary>
    ///     How much of the back is taken out, as a fraction of the length, so the beam trails away
    ///     rather than ending in a cut edge. The front is not faded, because that is where the shot
    ///     is and it carries the head.
    /// </summary>
    public const float EndFade = 0.42f;

    /// <summary>
    ///     The balls of plasma at either end of the shot. Both ends are light, like the rest of it,
    ///     rather than a sprite.
    /// </summary>
    /// <remarks>
    ///     <see cref="HeadStretch" /> squashes them along the beam, so they read as balls drawn out
    ///     into the shot rather than beads stuck on its ends. Below one they stretch so far as to
    ///     become a second beam.
    /// </remarks>
    public const float HeadSize = 0.85f;

    /// <inheritdoc cref="HeadSize" />
    public const float HeadStretch = 2.6f;

    /// <inheritdoc cref="HeadSize" />
    public const float HeadBoost = 1.35f;

    /// <summary>
    ///     How much of that the trailing ball gets. The shot is at the front and the back is only its
    ///     wake, so a matched pair reads as a dumbbell flying sideways rather than something
    ///     travelling.
    /// </summary>
    public const float BackShare = 0.62f;

    /// <summary>The single knob for how hard the whole beam burns.</summary>
    /// <remarks>
    ///     Deliberately over one. The beam is drawn additively, so this is how much light it adds
    ///     rather than an opacity, and there is nothing to clip against short of white.
    ///     <para>
    ///         If the shot ever looks too much, check what else is drawn on it before turning this
    ///         down. <c>FireTrail</c> balloons every particle to 256 pixels, so its count and
    ///         lifetime wash the screen out far more readily than this does.
    ///     </para>
    /// </remarks>
    public const float Intensity = 1.15f;

    /// <summary>What burns down the middle of the shot.</summary>
    public static Vector3 CoreColour => new(1f, 0.97f, 0.9f);

    /// <summary>
    ///     What spreads off it. The cyan the redrawn flame already uses, so the spread is more of the
    ///     same light rather than a second colour.
    /// </summary>
    public static Vector3 EdgeColour => new(0.3f, 0.8f, 0.95f);

    /// <summary>
    ///     Where the waves have travelled to by a given logic frame. Logic frames, so the beam
    ///     writhes at the same rate whatever the display is doing.
    /// </summary>
    public static float WavePhaseAt(int logicFrame) => (float)(logicFrame * WaveSpeed);

    /// <summary>
    ///     How long, thick and hard a shot burns, depending on whether Blob is firing from a shuttle
    ///     pad or on foot.
    /// </summary>
    public static (int Length, int Thickness, float Intensity) BeamFor(bool onShuttle)
        => onShuttle
            ? (Length, Thickness, Intensity)
            : ((int)(Length * OnFootLength),
                (int)(Thickness * OnFootThickness),
                Intensity * OnFootIntensity);
}
