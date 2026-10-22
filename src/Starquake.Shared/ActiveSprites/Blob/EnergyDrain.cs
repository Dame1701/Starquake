// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Constants;
using Starquake.Core;
using Starquake.Tuning;

namespace Starquake.ActiveSprites;

/// <summary>
///     How far gone Blob's energy is while a nasty has hold, and how it comes back once one lets go.
///     Stepped once a logic frame and read by the three things that draw Blob red.
/// </summary>
/// <remarks>
///     <see cref="Level" /> counts the green and blue remaining, so it is full when unhurt and nought
///     when a nasty has taken all of it. That is the opposite of what the shader wants, which is what
///     <see cref="EnergyDrainSettings.DrainFrom" /> is for.
///     <para>
///         The two directions are deliberately asymmetric. Being touched snaps the level to full
///         before taking it down, so every drain starts from the same place; letting go only climbs.
///         Without the snap, a nasty catching Blob twice in quick succession would carry on from
///         where the first drain stopped and finish him far faster than one touch should.
///     </para>
/// </remarks>
internal sealed class EnergyDrain
{
    /// <summary>
    ///     How fast Blob reddens while touched and recovers when not, in colour channel levels a
    ///     frame.
    /// </summary>
    private const int FadeStep = 16;

    /// <summary>
    ///     Whether a nasty had hold last frame, which is what makes the first frame of a touch
    ///     different from the rest.
    /// </summary>
    private bool _wasTouched;

    /// <summary>The green and blue remaining. Full is unhurt.</summary>
    public int Level { get; private set; } = ColourLevels.Full;

    /// <summary>
    ///     Logic frames since the game began, for the flicker on the wash. Counted on update and
    ///     never in draw: <c>GameHost</c> draws once per vertical retrace, so a flicker counted in
    ///     drawn frames crackles faster on a better monitor.
    /// </summary>
    public int Frame { get; private set; }

    /// <summary>Whether there is any drain to draw. Nearly always there is not.</summary>
    public bool Draining => EnergyDrainSettings.IsDraining(Level);

    /// <summary>One logic frame of draining or recovering.</summary>
    /// <param name="touchedByNasty">Whether a nasty has hold of Blob this frame.</param>
    public void Step(bool touchedByNasty)
    {
        Frame++;

        if (!touchedByNasty)
        {
            _wasTouched = false;
            Level = Ramp.Up(Level, FadeStep, ColourLevels.Full);

            return;
        }

        if (!_wasTouched)
        {
            Level = ColourLevels.Full;
            _wasTouched = true;
        }

        Level = Ramp.Down(Level, FadeStep, ColourLevels.None);
    }

    /// <summary>Back to unhurt, for a new game.</summary>
    public void Reset()
    {
        Level = ColourLevels.Full;
        _wasTouched = false;
    }
}
