// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -

namespace Starquake.ContentManagement;

public class ContentEntry
{
    public readonly string FilePath;
    public readonly string Name;

    public ContentEntry(string contentName, string contentFilePath)
    {
        Name = contentName;
        FilePath = contentFilePath;
    }
}