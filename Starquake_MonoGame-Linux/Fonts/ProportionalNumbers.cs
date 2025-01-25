using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StarquakeXNA.Fonts
{
    public class ProportionalNumbers
    {
        public static void Draw(int xPos, int yPos, string numbers, GameEngine gameEngine, GameTime gameTime, Color color)
        {
            gameEngine.ContentResourceManager.GetSpriteCollection("ProportionalNumbers").Begin(BlendState.AlphaBlend, gameTime);
            
            for (int a = 0; a < numbers.Length; a++)
            {
                string subScore = numbers.Substring(a, 1);

                if (subScore == ".")
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection("ProportionalNumbers").DrawSprite(10, xPos + (27 * a), yPos + 6, color);
                }
                else if (subScore == "%")
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection("ProportionalNumbers").DrawSprite(11, xPos + (27 * a), yPos + 6, color);
                }
                else
                {
                    if (subScore != " ") // Do nothing for a space
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection("ProportionalNumbers").DrawSprite(Int32.Parse(subScore), xPos + (27 * a), yPos + 6, color);
                    }
                }
            }
            gameEngine.ContentResourceManager.GetSpriteCollection("ProportionalNumbers").End();
        }
    }
}
