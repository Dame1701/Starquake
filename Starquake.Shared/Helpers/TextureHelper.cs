// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;

namespace Starquake.Helpers;

internal static class TextureHelper
{
    public static Texture2D Texture2DFromFile(GraphicsDevice graphicsDevice, string fileName)
    {
        using var stream = new FileStream(fileName, FileMode.Open);
        Texture2D texture = Texture2D.FromStream(graphicsDevice, stream);
        stream.Close();

        return texture;
    }
}
