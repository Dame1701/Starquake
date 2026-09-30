// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Starquake.Engines;

public class RenderViewPort
{
    private readonly GraphicsDeviceManager _graphicsDeviceManager;

    public RenderViewPort(GraphicsDeviceManager graphics, string name, int xPos, int yPos, int width, int height)
    {
        _graphicsDeviceManager = graphics;

        MasterXPos = xPos;
        MasterYPos = yPos;

        Width = width;
        Height = height;

        RenderTarget = new RenderTarget2D(_graphicsDeviceManager.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);


        XPos = xPos;
        YPos = yPos;
        Name = name;
        Active = false;
    }
    public RenderTarget2D RenderTarget { get; }

    public bool Active { get; set; }

    public int XPos { get; set; }

    public int YPos { get; set; }

    public int MasterXPos { get; }

    public int MasterYPos { get; }

    public int Width { get; set; }

    public int Height { get; }

    public int MasterWidth => RenderTarget.Width;

    public int MasterHeight => RenderTarget.Height;
    public string Name { get; }

    public Rectangle GetBounds()
        => new(XPos, YPos, Width, Height);
}
