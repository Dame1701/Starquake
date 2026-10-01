// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Constants;
using Starquake.ContentManagement;

namespace Starquake.Fonts;

internal static class ProportionalNumbers
{
    public static void Draw(int xPos, int yPos, string numbers, ResourceManager resources, Color color)
    {
        resources.GetSpriteCollection(Assets.Graphics.ProportionalNumbers).Begin(BlendState.AlphaBlend);

        for (int a = 0; a < numbers.Length; a++)
        {
            string subScore = numbers.Substring(a, 1);

            if (subScore == ".")
            {
                resources.GetSpriteCollection(Assets.Graphics.ProportionalNumbers).DrawSprite(10, xPos + 27 * a, yPos + 6, color);
            }
            else if (subScore == "%")
            {
                resources.GetSpriteCollection(Assets.Graphics.ProportionalNumbers).DrawSprite(11, xPos + 27 * a, yPos + 6, color);
            }
            else
            {
                if (subScore != " ") // Do nothing for a space
                {
                    resources.GetSpriteCollection(Assets.Graphics.ProportionalNumbers).DrawSprite(int.Parse(subScore), xPos + 27 * a, yPos + 6, color);
                }
            }
        }
        resources.GetSpriteCollection(Assets.Graphics.ProportionalNumbers).End();
    }
}
