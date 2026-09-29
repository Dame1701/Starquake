// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.Sprites;

public class SpritePixel
{
    public SpritePixel()
    {
        IsOn = true;
        Colour = Color.White;
    }
    public bool IsOn { get; set; }

    public Color Colour { get; set; }
}