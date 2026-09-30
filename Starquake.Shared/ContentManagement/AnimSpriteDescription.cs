// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -

namespace Starquake.ContentManagement;

internal class AnimSpriteDescription
{
    public readonly int Delay;
    public readonly string FilePath;
    public readonly int Height;
    public readonly string ResourceName;
    public readonly int TileToAnimate;
    public readonly int Width;

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