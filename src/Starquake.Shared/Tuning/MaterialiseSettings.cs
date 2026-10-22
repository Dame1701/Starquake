// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;

namespace Starquake.Tuning;

/// <summary>
///     What a nasty arriving looks like, and the one sum behind it. Driven by
///     <c>Shaders/Materialise.fx</c>; see <c>Nasty.DrawBirth</c> for where it is handed over.
/// </summary>
/// <remarks>
///     The birth already spins the nasty down to square, grows it from nothing and brings it from
///     blue into its own colours over sixty logic frames. The shader adds the two things a sprite
///     batch cannot: sampling the three colour channels apart from one another, and revealing the
///     sprite by part of itself rather than by its alpha as a whole. The two lines open out from its
///     middle, held still against the screen while the nasty spins under them.
/// </remarks>
internal static class MaterialiseSettings
{
    /// <summary>
    ///     How far apart the red and blue channels are pulled as a nasty starts to arrive, as a
    ///     fraction of the sprite. They converge to nothing as it settles.
    /// </summary>
    public const float ChromaticSpread = 0.22f;

    /// <summary>
    ///     How soft the edge of the arriving lines is, as a fraction of the sprite's height. Too
    ///     hard and it reads as a wipe; too soft and it reads as a fade with no line in it.
    /// </summary>
    public const float ScanSoftness = 0.1f;

    /// <summary>How wide the hot part of each line is, as a fraction of the height.</summary>
    public const float BandWidth = 0.22f;

    /// <summary>
    ///     How hard the lines burn. Over one on purpose, so they bloom in the cathode ray tube rather
    ///     than merely being bright. The nasty spins past at two revolutions a second, so anything
    ///     subtle on it is not seen at all.
    /// </summary>
    public const float BandBrightness = 2.4f;

    /// <summary>
    ///     What the arriving lines glow. The same blue the birth brings a nasty in on, so they look
    ///     like where the colour is arriving from rather than a second idea.
    /// </summary>
    public static Vector3 BandColour => new(0.55f, 0.8f, 1f);

    /// <summary>How far through arriving a nasty is, from nought to one.</summary>
    /// <remarks>
    ///     Clamped at both ends because the birth runs past its own period: it keeps spinning for up
    ///     to <c>BirthSpinOutLimit</c> frames waiting to come to rest square, and a progress past one
    ///     would start pulling the channels apart again the other way.
    /// </remarks>
    public static float ProgressThrough(int frame, int period)
        => Math.Clamp(frame / (float)period, 0f, 1f);
}
