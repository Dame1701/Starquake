// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace StarquakeXNA.Helpers;

public static class TextureHelper
{
    public static Texture2D Texture2DFromFile(GraphicsDevice graphicsDevice, string fileName)
    {
        Texture2D texture;
        using (var stream = new FileStream(fileName, FileMode.Open))
        {
            texture = Texture2D.FromStream(graphicsDevice, stream);
            stream.Close();
        }

        return texture;
    }
}