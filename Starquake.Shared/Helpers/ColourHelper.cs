using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace StarquakeXNA.Helpers
{
    public class ColourHelper
    {
        public static Color GetXNA4Colour(int r, int g, int b, int a)
        {
            return new Color(r, g, b) * (float)((1f / 255f) * (float)a);
        }

        public static Color GetXNA4Colour(int r, int g, int b)
        {
            return new Color(r, g, b) * 1;
        }
    }
}
