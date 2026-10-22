// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Constants;
using Starquake.Core;

namespace Starquake.ActiveSprites;

/// <summary>
///     Blob dissolving into a teleport beam and returning from one: how solid Blob is drawn, and
///     whether the beam is still playing.
/// </summary>
/// <remarks>
///     Two values rather than one stage, because these are not one animation. The beam is a particle
///     effect that runs as long as it runs and reports when it finishes; the fade climbs from nothing
///     back to solid at a fixed rate. They start together and end independently.
///     <para>
///         The beam itself stays with <c>Blob</c>, which has the resource manager it needs and a
///         position to draw it at each frame.
///     </para>
/// </remarks>
internal sealed class TeleportFade
{
    /// <summary>How fast Blob fades back in, in colour channel levels a frame.</summary>
    private const int FadeStep = 8;

    /// <summary>
    ///     How solid Blob is drawn, from nothing on arrival to <see cref="ColourLevels.Full" /> once
    ///     all there. Starts at full, since no teleport has happened yet.
    /// </summary>
    public int Level { get; private set; } = ColourLevels.Full;

    /// <summary>
    ///     Whether the beam is still playing. Set when the teleport starts and cleared by the
    ///     particle effect itself, which is the only thing that knows when it has finished.
    /// </summary>
    public bool BeamPlaying { get; private set; }

    /// <summary>
    ///     Blob has stepped into a booth. The beam starts, and Blob is drawn from nothing upwards.
    /// </summary>
    public void Begin()
    {
        BeamPlaying = true;
        Level = ColourLevels.None;
    }

    /// <summary>Called by the particle effect when the beam has run out.</summary>
    public void BeamFinished() => BeamPlaying = false;

    /// <summary>
    ///     One logic frame of fading back in. Safe to call every frame: a level already at full does
    ///     not move.
    /// </summary>
    public void Step() => Level = Ramp.Up(Level, FadeStep, ColourLevels.Full);
}
