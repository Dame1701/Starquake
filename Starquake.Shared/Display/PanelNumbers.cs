// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Engines;

namespace Starquake.Display;

public class PanelNumbers
{
    public void Draw(int xPos, int yPos, string score, GameEngine gameEngine, GameTime gameTime, Color color)
    {
        gameEngine.ContentResourceManager.GetSpriteCollection("PanelNumbers").Begin(BlendState.AlphaBlend, gameTime);
        for (var a = 0; a < score.Length; a++)
        {
            var subScore = score.Substring(a, 1);
            gameEngine.ContentResourceManager.GetSpriteCollection("PanelNumbers").DrawSprite(int.Parse(subScore), xPos + 31 * a, yPos, color);
        }
        gameEngine.ContentResourceManager.GetSpriteCollection("PanelNumbers").End();
    }
}