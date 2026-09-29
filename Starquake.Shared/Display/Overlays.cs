// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Engines;
using Starquake.Helpers;

namespace Starquake.Display;

public class Overlays
{
    public static void Paused(int xPos, int yPos, GameEngine gameEngine)
    {
        gameEngine.ShapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(0 + xPos, 0 + yPos, 244, 10), ColourHelper.GetXna4Colour(100, 0, 0, 200));
        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(0 + xPos, 10 + yPos, 10, 62), ColourHelper.GetXna4Colour(100, 0, 0, 200));
        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(12 + xPos, 12 + yPos, 220, 58), ColourHelper.GetXna4Colour(20, 20, 20, 200));
        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(0 + xPos, 72 + yPos, 244, 10), ColourHelper.GetXna4Colour(100, 0, 0, 200));
        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(234 + xPos, 10 + yPos, 10, 62), ColourHelper.GetXna4Colour(100, 0, 0, 200));
        gameEngine.ShapeDrawer.End();

        gameEngine.Font2.Begin(BlendState.AlphaBlend);
        gameEngine.Font2.WriteText(42 + xPos, 22 + yPos, "PAUSED...", 2, Color.White, 255);
        gameEngine.Font2.End();
    }
}