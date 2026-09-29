// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace StarquakeXNA;

[Serializable]
[XmlRoot]
public class XNAFont : IDisposable
{

    private string fontFamily;

    [XmlIgnore] private SpriteBatch fontRenderer;

    private int height;

    private int kerning;

    [XmlIgnore]
    public Effect refractionEffect;
    [XmlIgnore]
    public Texture2D Texture;

    [XmlIgnore]
    public Dictionary<char, XNAFontUV> UVData = new();

    public List<XNAFontUV> UVList;

    public Texture2D waterFall;

    public string FontFamily
    {
        get => fontFamily;
        set => fontFamily = value;
    }

    public int Height
    {
        get => height;
        set => height = value;
    }

    public int Kerning
    {
        get => kerning;
        set => kerning = value;
    }

    #region IDisposable Members

    public void Dispose()
    {
        if (Texture != null)
            Texture.Dispose();

        if (fontRenderer != null)
            fontRenderer.Dispose();

        Texture = null;
        fontRenderer = null;
    }

    #endregion

    public void OutputText(string str, int x, int y, Color col)
    {
        OutputText(str, x, y, col, null);
    }

    public void OutputText(string str, int x, int y, Color col, GameTime gameTime)
    {
        var iCurrentX = x;
        var iCurrentY = y;

        // Draw the sprite.
        fontRenderer.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

        if (gameTime != null)
        {
            refractionEffect.Parameters["DisplacementScroll"].SetValue(MoveInCircle(gameTime, 0.1f));

            // Begin the custom effect.
            refractionEffect.CurrentTechnique.Passes[0].Apply();
        }


        foreach (var c in str)
        {
            var uv = UVData[c];

            if (c != ' ')
            {
                fontRenderer.Draw(Texture,
                    new Rectangle(iCurrentX, iCurrentY,
                        uv.Width, height),
                    new Rectangle(uv.V - 2,
                        uv.U,
                        uv.Width, height),
                    col);
            }

            iCurrentX += uv.Width - kerning;
        }
        fontRenderer.End();
    }

    /// <summary>
    ///     Helper for moving a value around in a circle.
    /// </summary>
    private static Vector2 MoveInCircle(GameTime gameTime, float speed)
    {
        var time = gameTime.TotalGameTime.TotalSeconds * speed;

        var x = (float)Math.Cos(time);
        var y = (float)Math.Sin(time);

        return new Vector2(x, y);
    }

    public static XNAFont LoadFont(GraphicsDevice device, string strXML, Texture2D tex, ContentManager Content)
    {
        var fontLoader = new XmlSerializer(typeof(XNAFont));

        Stream reader = new FileStream(strXML, FileMode.Open);

        var ret = (XNAFont)fontLoader.Deserialize(reader);
        ret.waterFall = Content.Load<Texture2D>("Graphics\\waterfall");

        device.Textures[1] = ret.waterFall;

        reader.Close();

        ret.refractionEffect = Content.Load<Effect>("Shaders\\refraction");
        ret.UVData = new Dictionary<char, XNAFontUV>();
        foreach (var uv in ret.UVList)
        {
            ret.UVData.Add(uv.C, uv);
        }

        ret.UVList = null;

        ret.Texture = tex;
        ret.fontRenderer = new SpriteBatch(device);

        return ret;
    }

    [Serializable]
    public class XNAFontUV
    {

        [XmlAttribute]
        public char C;
        [XmlAttribute]
        public int U;

        [XmlAttribute]
        public int V;

        [XmlAttribute]
        public int Width;

        public XNAFontUV()
        {
            U = 0;
            V = 0;
            Width = 0;
            C = (char)0;
        }

        public XNAFontUV(int u, int v, char c, int Width)
        {
            U = u;
            V = v;
            C = c;
            this.Width = Width;
        }
    }
}