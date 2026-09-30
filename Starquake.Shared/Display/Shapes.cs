// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Starquake.Display;

/// <summary>
///     An extended version of the SpriteBatch class that supports line and
///     rectangle drawing.
/// </summary>
public class Shapes : SpriteBatch
{

    public Shapes(GraphicsDevice graphicsDevice)
        : base(graphicsDevice)
    {
        WhiteTexture = new Texture2D(GraphicsDevice, 1, 1);
        WhiteTexture.SetData([Color.White]);
    }
    /// <summary>
    ///     The texture used when drawing rectangles, lines and other
    ///     primitives. This is a 1x1 white texture created at runtime.
    /// </summary>
    private Texture2D WhiteTexture { get; }

    /// <summary>
    ///     Fill a rectangle.
    /// </summary>
    /// <param name="rectangle">The rectangle to fill.</param>
    /// <param name="color">The fill color.</param>
    public void FillRectangle(Rectangle rectangle, Color color)
    {
        Draw(WhiteTexture, rectangle, color);
    }
}
