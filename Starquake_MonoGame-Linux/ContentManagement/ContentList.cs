using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content;

namespace StarquakeXNA.ContentManagement
{
    public class ContentList
    {
        private List<ContentEntry> content;
        
        private ContentManager contentMgr;
        public ContentManager ContentMgr
        {
            get { return contentMgr; }
        }

        public ContentList(ContentManager ContentMgr)
        {
            content = new List<ContentEntry>();
            contentMgr = ContentMgr;
        }

        public void AddContent(string ContentName, string ContentFilePath)
        {
            content.Add(new ContentEntry(ContentName, ContentFilePath));
        }

        public string GetContent(string ContentName)
        {
            for (int a = 0; a < content.Count; a++)
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
            for (int a = 0; a < content.Count; a++)
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
        public string Name;
        public string FilePath;

        public ContentEntry(string ContentName, string ContentFilePath)
        {
            Name = ContentName;
            FilePath = ContentFilePath;
        }
    }
}
