// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Constants;
using Starquake.ContentManagement;

namespace Starquake.Display;

internal static class PanelNumbers
{
    public static void Draw(int xPos, int yPos, string score, ResourceManager resources, Color color)
    {
        resources.GetSpriteCollection(Assets.Graphics.PanelNumbers).Begin(BlendState.AlphaBlend);
        for (int a = 0; a < score.Length; a++)
        {
            string subScore = score.Substring(a, 1);
            resources.GetSpriteCollection(Assets.Graphics.PanelNumbers).DrawSprite(int.Parse(subScore), xPos + 31 * a, yPos, color);
        }
        resources.GetSpriteCollection(Assets.Graphics.PanelNumbers).End();
    }
}
