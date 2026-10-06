// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.Tuning;

namespace Starquake.ActiveSprites;

/// <summary>
///     How far gone Blob's energy is while a nasty has hold of him, and how it comes back when one
///     lets go. Stepped once a logic frame, and read by the three things that draw him red.
/// </summary>
/// <remarks>
///     <see cref="Level" /> is the green and blue he has left, so it reads full when he is unhurt
///     and nought when a nasty has taken all of it - the other way round from what the shader wants,
///     which is what <see cref="EnergyDrainSettings.DrainFrom" /> is for.
///     <para>
///         The two directions are not symmetrical and that is the whole of the feel. Being touched
///         snaps the level to full first and then takes it down, so the drain starts from a known
///         place however drained he already was; letting go only climbs. Without the snap, a nasty
///         catching him twice in quick succession would start the second drain from wherever the
///         first left off and finish him far faster than one touch should.
///     </para>
/// </remarks>
internal sealed class EnergyDrain
{
    /// <summary>
    ///     How fast Blob reddens while a nasty is touching him and recovers when one is not,
    ///     counted in colour channel levels a frame.
    /// </summary>
    private const int FadeStep = 16;

    /// <summary>
    ///     Whether a nasty had hold of him last frame, which is what makes the first frame of a
    ///     touch different from the rest of it.
    /// </summary>
    private bool _wasTouched;

    /// <summary>
    ///     The green and blue he has left. Full is unhurt.
    /// </summary>
    public int Level { get; private set; } = ColourLevels.Full;

    /// <summary>
    ///     Logic frames since the game began, for the flicker on the wash. Counted on the update and
    ///     never in the draw: <c>GameHost</c> draws once per vertical retrace, so a flicker counted
    ///     in drawn frames crackles faster on a better monitor.
    /// </summary>
    public int Frame { get; private set; }

    /// <summary>
    ///     Whether there is any drain to draw at all. Nearly always there is not.
    /// </summary>
    public bool Draining => EnergyDrainSettings.IsDraining(Level);

    /// <summary>
    ///     One logic frame of it.
    /// </summary>
    /// <param name="touchedByNasty">Whether a nasty has hold of him this frame.</param>
    public void Step(bool touchedByNasty)
    {
        Frame++;

        if (!touchedByNasty)
        {
            _wasTouched = false;
            Level = Math.Min(Level + FadeStep, ColourLevels.Full);

            return;
        }

        if (!_wasTouched)
        {
            Level = ColourLevels.Full;
            _wasTouched = true;
        }

        Level = Math.Max(Level - FadeStep, ColourLevels.None);
    }

    /// <summary>
    ///     Back to unhurt, for a new game.
    /// </summary>
    public void Reset()
    {
        Level = ColourLevels.Full;
        _wasTouched = false;
    }
}
