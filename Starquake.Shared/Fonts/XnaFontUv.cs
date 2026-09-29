// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Xml.Serialization;

namespace Starquake.Fonts;

[Serializable]
public class XnaFontUv
{

    [XmlAttribute]
    public char C;
    [XmlAttribute]
    public int U;

    [XmlAttribute]
    public int V;

    [XmlAttribute]
    public int Width;

    public XnaFontUv()
    {
        U = 0;
        V = 0;
        Width = 0;
        C = (char)0;
    }

    public XnaFontUv(int u, int v, char c, int width)
    {
        U = u;
        V = v;
        C = c;
        this.Width = width;
    }
}
