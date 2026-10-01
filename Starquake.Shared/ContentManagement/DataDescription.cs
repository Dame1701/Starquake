// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.ContentManagement;

internal class DataDescription
{
    public readonly string FilePath;
    public readonly string ResourceName;

    public DataDescription(string resourceName, string filePath)
    {
        ResourceName = resourceName;
        FilePath = filePath;
    }
}
