// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Starquake.Display;

/// <summary>A <c>SpriteBatch</c> that can also draw lines and rectangles.</summary>
internal class Shapes : SpriteBatch
{

    public Shapes(GraphicsDevice graphicsDevice)
        : base(graphicsDevice)
    {
        WhiteTexture = new Texture2D(GraphicsDevice, 1, 1);
        WhiteTexture.SetData([Color.White]);
    }
    /// <summary>
    ///     A 1x1 white texture, built at runtime, stretched to draw every line and rectangle.
    /// </summary>
    private Texture2D WhiteTexture { get; }

    /// <summary>Fills a rectangle.</summary>
    public void FillRectangle(Rectangle rectangle, Color color)
    {
        Draw(WhiteTexture, rectangle, color);
    }
}
