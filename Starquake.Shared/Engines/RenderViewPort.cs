// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Starquake.Engines;

internal class RenderViewPort
{

    public RenderViewPort(GraphicsDeviceManager graphics, string name, int xPos, int yPos, int width, int height)
    {
        MasterXPos = xPos;
        MasterYPos = yPos;

        Width = width;
        Height = height;

        RenderTarget = new RenderTarget2D(graphics.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);


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

    public string Name { get; }

}
