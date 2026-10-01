// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.ContentManagement;

internal class ContentList
{
    private readonly List<ContentEntry> _content;

    public ContentList()
    {
        _content = new List<ContentEntry>();
    }

    public void AddContent(string contentName, string contentFilePath)
    {
        _content.Add(new ContentEntry(contentName, contentFilePath));
    }

    public string GetContent(string contentName)
    {
        foreach (ContentEntry content in _content)
        {
            if (content.Name == contentName)
            {
                return content.FilePath;
            }
        }

        throw new InvalidOperationException($"Content '{contentName}' has not been added to the list.");
    }

}
