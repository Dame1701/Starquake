// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -

namespace Starquake.ContentManagement;

internal class SpriteDescription
{
    public string FilePath;
    public int Height;
    public string ResourceName;
    public int Width;

    public SpriteDescription(string resourceName, string filePath, int width, int height)
    {
        ResourceName = resourceName;
        FilePath = filePath;
        Width = width;
        Height = height;
    }
}