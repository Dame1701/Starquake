// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.Core;

/// <summary>
///     One pulsing tile: which tile, what colour it pulses towards, how fast, and how far through
///     its current pulse it is.
/// </summary>
/// <remarks>
///     The running state lives here rather than beside the registration, which is why
///     <c>ColourAnimatedTiles.All</c> hands out a fresh set on every call. A shared static set would
///     make the registered animations and the live ones the same objects.
/// </remarks>
internal class ColourFader
{
    /// <summary>
    ///     Which way the current pulse is going. A pulse fades up to
    ///     <see cref="TransitionColour" /> and back down once per loop.
    /// </summary>
    public ColourFaderDirection Direction;

    /// <summary>
    ///     How many times the tile is drawn over itself. The blend is additive, so a higher count
    ///     builds a stronger shade of the destination colour.
    /// </summary>
    public int Intensity;

    /// <summary>How many pulses to run before stopping, or -1 to pulse until stopped.</summary>
    public int LoopCount;

    /// <inheritdoc cref="LoopCount" />
    public int LoopCounter;

    /// <summary>Whether this animation is currently pulsing.</summary>
    public bool Running;

    /// <summary>How much the colour moves each frame, in channel levels.</summary>
    public int Speed;

    /// <summary>
    ///     Whether the animation has been asked to stop. It finishes the pulse it is in rather than
    ///     cutting off part-faded, which is why stopping is a flag rather than clearing
    ///     <see cref="Running" />.
    /// </summary>
    public bool Stopping;

    /// <summary>
    ///     The tile this animation pulses. Every tile on screen with this number pulses together.
    /// </summary>
    public int TileNo;

    /// <summary>The colour laid over the tile at the top of each pulse.</summary>
    public Color TransitionColour;

    /// <summary>
    ///     A second tile laid over the first as it pulses, or -1 for a pulse that repaints the tile
    ///     already there, as the shuttle tags do.
    /// </summary>
    public int UpperTileToDraw;

    /// <summary>
    ///     Where the lights sit within this tile, each of which throws a halo. Empty for a tile that
    ///     does not glow, which is everything but the shuttle tags.
    /// </summary>
    /// <remarks>
    ///     Positions rather than a flag, because the lights are not at a tile's middle. A tag cell is
    ///     128 by 96 and its lights are blobs of 28 by 36 at one end, with tag 417 having two at
    ///     opposite ends, so a halo centred on the tile would land in the middle of the launcher with
    ///     the lights dark at its edges.
    ///     <para>Measured off the artwork, so they move if the tiles are redrawn.</para>
    /// </remarks>
    public Point[] HaloLights = [];
}
