using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace StarquakeXNA.Helpers
{
    public static class TextureHelper
    {
        public static Texture2D Texture2DFromFile(GraphicsDevice graphicsDevice, string fileName)
        {
            Texture2D texture;
            using ( FileStream stream = new FileStream(fileName, FileMode.Open))
            {
                texture = Texture2D.FromStream(graphicsDevice, stream);
                stream.Close();
            }

            return texture;
        }
    }
}
