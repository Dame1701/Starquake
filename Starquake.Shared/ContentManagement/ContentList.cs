// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;

namespace Starquake.ContentManagement;

public class ContentList
{
    private readonly List<ContentEntry> _content;

    public ContentList(ContentManager contentMgr)
    {
        _content = new List<ContentEntry>();
        this.ContentMgr = contentMgr;
    }
    public ContentManager ContentMgr { get; }

    public void AddContent(string contentName, string contentFilePath)
    {
        _content.Add(new ContentEntry(contentName, contentFilePath));
    }

    public string GetContent(string contentName)
    {
        for (var a = 0; a < _content.Count; a++)
        {
            if (_content[a].Name == contentName)
            {
                return _content[a].FilePath;
            }
        }

        return null;
    }

    public void RemoveContent(string contentName)
    {
        for (var a = 0; a < _content.Count; a++)
        {
            if (_content[a].Name == contentName)
            {
                _content.Remove(_content[a]);
                break;
            }
        }
    }
}

public class ContentEntry
{
    public string FilePath;
    public string Name;

    public ContentEntry(string contentName, string contentFilePath)
    {
        Name = contentName;
        FilePath = contentFilePath;
    }
}