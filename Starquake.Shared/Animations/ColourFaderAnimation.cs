// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.Animations;

internal class ColourFaderAnimation
{

    // Animation direction
    public ColourAnimationDirection Direction;

    // We use additive blending to build up a more severe shade of destination colour
    public int Intensity;

    // Number of times to loop before stopping
    public int LoopCount;

    // Loop counter
    public int LoopCounter;

    // Is the animation running?
    public bool Running;

    // How fast to do the colour fade
    public int Speed;

    // Is the animation stopping
    public bool Stopping;

    // The tile to affect
    public int TileNo;

    // Colour we're overlaying
    public Color TransitionColour;

    // The tile index of the upper layer to draw
    public int UpperTileToDraw;
}
