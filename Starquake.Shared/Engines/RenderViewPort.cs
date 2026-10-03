// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Starquake.Engines;

internal class RenderViewPort(GraphicsDeviceManager graphics, string name, int xPos, int yPos, int width, int height)
{
    public RenderTarget2D RenderTarget { get; } = new(graphics.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);

    public bool Active { get; set; }

    public int XPos { get; set; } = xPos;

    public int YPos { get; set; } = yPos;

    public int MasterXPos { get; } = xPos;

    public int MasterYPos { get; } = yPos;

    public int Width { get; set; } = width;

    public int Height { get; } = height;

    public int MasterWidth => RenderTarget.Width;

    public string Name { get; } = name;
}
