// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;

namespace StarquakeXNA.ContentManagement;

public class ContentList
{
    private readonly List<ContentEntry> content;

    public ContentList(ContentManager ContentMgr)
    {
        content = new List<ContentEntry>();
        this.ContentMgr = ContentMgr;
    }
    public ContentManager ContentMgr { get; }

    public void AddContent(string ContentName, string ContentFilePath)
    {
        content.Add(new ContentEntry(ContentName, ContentFilePath));
    }

    public string GetContent(string ContentName)
    {
        for (var a = 0; a < content.Count; a++)
        {
            if (content[a].Name == ContentName)
            {
                return content[a].FilePath;
            }
        }

        return null;
    }

    public void RemoveContent(string ContentName)
    {
        for (var a = 0; a < content.Count; a++)
        {
            if (content[a].Name == ContentName)
            {
                content.Remove(content[a]);
                break;
            }
        }
    }
}

public class ContentEntry
{
    public string FilePath;
    public string Name;

    public ContentEntry(string ContentName, string ContentFilePath)
    {
        Name = ContentName;
        FilePath = ContentFilePath;
    }
}