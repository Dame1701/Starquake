// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.Animations;

/// <summary>
///     One pulsing tile: which tile, what colour it pulses towards, how fast, and how far through
///     its current pulse it is.
/// </summary>
/// <remarks>
///     The running state lives here rather than beside the registration, which is why
///     <see cref="ColourAnimatedTiles.All" /> hands out a fresh set every call - a shared static set
///     would have the registered animations and the live ones be the same objects.
/// </remarks>
internal class ColourFaderAnimation
{
    /// <summary>
    ///     Which way the current pulse is going. A pulse fades up to
    ///     <see cref="TransitionColour" /> and back down again, once per loop.
    /// </summary>
    public ColourAnimationDirection Direction;

    /// <summary>
    ///     How many times the tile is drawn over itself. The blend is additive, so a higher
    ///     intensity builds up a more severe shade of the destination colour.
    /// </summary>
    public int Intensity;

    /// <summary>
    ///     How many up-and-down pulses to run before stopping, or -1 to pulse until stopped.
    /// </summary>
    public int LoopCount;

    /// <inheritdoc cref="LoopCount" />
    public int LoopCounter;

    /// <summary>
    ///     Whether this animation is pulsing at the moment.
    /// </summary>
    public bool Running;

    /// <summary>
    ///     How much the colour moves each frame, in channel levels.
    /// </summary>
    public int Speed;

    /// <summary>
    ///     Whether the animation has been asked to stop. It finishes the pulse it is in rather than
    ///     cutting off part-faded, which is why stopping is a flag and not just clearing
    ///     <see cref="Running" />.
    /// </summary>
    public bool Stopping;

    /// <summary>
    ///     The tile this animation pulses. Every tile on the screen bearing this number pulses
    ///     together.
    /// </summary>
    public int TileNo;

    /// <summary>
    ///     The colour laid over the tile at the top of each pulse.
    /// </summary>
    public Color TransitionColour;

    /// <summary>
    ///     A second tile laid over the first as it pulses, or -1 for a pulse that simply repaints the
    ///     tile already there - which is what the shuttle tags do.
    /// </summary>
    public int UpperTileToDraw;
}
