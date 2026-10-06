// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.Core;

namespace Starquake.ActiveSprites;

/// <summary>
///     Blob dissolving into a teleport beam and coming back out of one: how solid he is drawn, and
///     whether the beam is still playing over him.
/// </summary>
/// <remarks>
///     The two halves are not one animation, which is why they are two values rather than a stage.
///     The beam is a particle effect that runs for as long as it runs and says when it is finished;
///     the fade is Blob climbing back from nothing to solid at a fixed rate. They start together
///     and they end whenever each of them ends.
///     <para>
///         The beam itself stays with <c>Blob</c> - it needs the resource manager to exist at all,
///         and a position every frame to be drawn at. What is here is only the two things that say
///         where in the business he is.
///     </para>
/// </remarks>
internal sealed class TeleportFade
{
    /// <summary>
    ///     How fast he fades back in, in colour channel levels a frame.
    /// </summary>
    private const int FadeStep = 8;

    /// <summary>
    ///     How solid Blob is drawn, from nothing at the moment he arrives to
    ///     <see cref="ColourLevels.Full" /> once he is all there. Full to begin with: he has not
    ///     teleported yet.
    /// </summary>
    public int Level { get; private set; } = ColourLevels.Full;

    /// <summary>
    ///     Whether the beam is still playing. Set when the teleport starts and cleared by the
    ///     particle effect itself, which is the only thing that knows when it has finished.
    /// </summary>
    public bool BeamPlaying { get; private set; }

    /// <summary>
    ///     He has stepped into a booth and is going somewhere: the beam starts and he is drawn out
    ///     of nothing from here.
    /// </summary>
    public void Begin()
    {
        BeamPlaying = true;
        Level = ColourLevels.None;
    }

    /// <summary>
    ///     Told by the particle effect when the beam has run out.
    /// </summary>
    public void BeamFinished() => BeamPlaying = false;

    /// <summary>
    ///     One logic frame of fading back in. Safe to call every frame whether or not he has
    ///     teleported - a level already at full does not move.
    /// </summary>
    public void Step() => Level = Ramp.Up(Level, FadeStep, ColourLevels.Full);
}
