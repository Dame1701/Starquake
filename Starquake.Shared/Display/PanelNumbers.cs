// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.ContentManagement;

namespace Starquake.Display;

internal class PanelNumbers
{
    public static void Draw(int xPos, int yPos, string score, ResourceManager resources, Color color)
    {
        resources.GetSpriteCollection("PanelNumbers").Begin(BlendState.AlphaBlend);
        for (int a = 0; a < score.Length; a++)
        {
            string subScore = score.Substring(a, 1);
            resources.GetSpriteCollection("PanelNumbers").DrawSprite(int.Parse(subScore), xPos + 31 * a, yPos, color);
        }
        resources.GetSpriteCollection("PanelNumbers").End();
    }
}
