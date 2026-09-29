// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace StarquakeXNA.Helpers;

public class ColourHelper
{
    public static Color GetXNA4Colour(int r, int g, int b, int a)
        => new Color(r, g, b) * (1f / 255f * a);

    public static Color GetXNA4Colour(int r, int g, int b)
        => new Color(r, g, b) * 1;
}