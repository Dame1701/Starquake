using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Helpers;
using Microsoft.Xna.Framework;

namespace StarquakeXNA.Display
{
    public class Overlays
    {
        public static void Paused(int XPos, int YPos, GameEngine gameEngine)
        {
            gameEngine.ShapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
            gameEngine.ShapeDrawer.FillRectangle(new Rectangle(0 + XPos, 0 + YPos, 244, 10), ColourHelper.GetXNA4Colour(100, 0, 0, 200));
            gameEngine.ShapeDrawer.FillRectangle(new Rectangle(0 + XPos, 10 + YPos, 10, 62), ColourHelper.GetXNA4Colour(100, 0, 0, 200));
            gameEngine.ShapeDrawer.FillRectangle(new Rectangle(12 + XPos, 12 + YPos, 220, 58), ColourHelper.GetXNA4Colour(20, 20, 20, 200));
            gameEngine.ShapeDrawer.FillRectangle(new Rectangle(0 + XPos, 72 + YPos, 244, 10), ColourHelper.GetXNA4Colour(100, 0, 0, 200));
            gameEngine.ShapeDrawer.FillRectangle(new Rectangle(234 + XPos, 10 + YPos, 10, 62), ColourHelper.GetXNA4Colour(100, 0, 0, 200));
            gameEngine.ShapeDrawer.End();

            gameEngine.Font2.Begin(BlendState.AlphaBlend);
            gameEngine.Font2.WriteText(42 + XPos, 22 + YPos, "PAUSED...", 2, Color.White, 255);
            gameEngine.Font2.End();
        }
    }
}
