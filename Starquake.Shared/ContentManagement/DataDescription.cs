// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;

namespace Starquake.ContentManagement;

internal class DataDescription
{
    public readonly string FilePath;
    public readonly string ResourceName;
    public Type ResourceType;

    public DataDescription(string resourceName, string filePath, Type resourceType)
    {
        ResourceName = resourceName;
        FilePath = filePath;
        ResourceType = resourceType;
    }
}