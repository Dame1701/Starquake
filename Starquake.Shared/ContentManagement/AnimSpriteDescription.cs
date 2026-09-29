// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -

namespace Starquake.ContentManagement;

internal class AnimSpriteDescription
{
    public int Delay;
    public string FilePath;
    public int Height;
    public string ResourceName;
    public int TileToAnimate;
    public int Width;

    public AnimSpriteDescription(string resourceName, string filePath, int width, int height, int tileToAnimate, int delay)
    {
        ResourceName = resourceName;
        FilePath = filePath;
        Width = width;
        Height = height;
        TileToAnimate = tileToAnimate;
        Delay = delay;
    }
}