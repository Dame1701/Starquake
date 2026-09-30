// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.ContentManagement;

namespace Starquake.Fonts;

public class ProportionalNumbers
{
    public static void Draw(int xPos, int yPos, string numbers, ResourceManager resources, GameTime gameTime, Color color)
    {
        resources.GetSpriteCollection("ProportionalNumbers").Begin(BlendState.AlphaBlend, gameTime);

        for (var a = 0; a < numbers.Length; a++)
        {
            var subScore = numbers.Substring(a, 1);

            if (subScore == ".")
            {
                resources.GetSpriteCollection("ProportionalNumbers").DrawSprite(10, xPos + 27 * a, yPos + 6, color);
            }
            else if (subScore == "%")
            {
                resources.GetSpriteCollection("ProportionalNumbers").DrawSprite(11, xPos + 27 * a, yPos + 6, color);
            }
            else
            {
                if (subScore != " ") // Do nothing for a space
                {
                    resources.GetSpriteCollection("ProportionalNumbers").DrawSprite(int.Parse(subScore), xPos + 27 * a, yPos + 6, color);
                }
            }
        }
        resources.GetSpriteCollection("ProportionalNumbers").End();
    }
}