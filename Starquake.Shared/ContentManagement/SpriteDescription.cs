// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.ContentManagement;

internal class SpriteDescription
{
    public readonly string FilePath;
    public readonly int Height;
    public readonly string ResourceName;
    public readonly int Width;

    public SpriteDescription(string resourceName, string filePath, int width, int height)
    {
        ResourceName = resourceName;
        FilePath = filePath;
        Width = width;
        Height = height;
    }
}
