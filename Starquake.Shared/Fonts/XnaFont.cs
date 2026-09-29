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

namespace StarquakeXNA.Fonts;

[Serializable]
[XmlRoot]
public class XnaFont : IDisposable
{

    private string _fontFamily;

    [XmlIgnore] private SpriteBatch _fontRenderer;

    private int _height;

    private int _kerning;

    [XmlIgnore]
    public Effect RefractionEffect;
    [XmlIgnore]
    public Texture2D Texture;

    [XmlIgnore]
    public Dictionary<char, XnaFontUv> UvData = new();

    public List<XnaFontUv> UvList;

    public Texture2D WaterFall;

    public string FontFamily
    {
        get => _fontFamily;
        set => _fontFamily = value;
    }

    public int Height
    {
        get => _height;
        set => _height = value;
    }

    public int Kerning
    {
        get => _kerning;
        set => _kerning = value;
    }

    #region IDisposable Members

    public void Dispose()
    {
        if (Texture != null)
            Texture.Dispose();

        if (_fontRenderer != null)
            _fontRenderer.Dispose();

        Texture = null;
        _fontRenderer = null;
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
        _fontRenderer.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

        if (gameTime != null)
        {
            RefractionEffect.Parameters["DisplacementScroll"].SetValue(MoveInCircle(gameTime, 0.1f));

            // Begin the custom effect.
            RefractionEffect.CurrentTechnique.Passes[0].Apply();
        }


        foreach (var c in str)
        {
            var uv = UvData[c];

            if (c != ' ')
            {
                _fontRenderer.Draw(Texture,
                    new Rectangle(iCurrentX, iCurrentY,
                        uv.Width, _height),
                    new Rectangle(uv.V - 2,
                        uv.U,
                        uv.Width, _height),
                    col);
            }

            iCurrentX += uv.Width - _kerning;
        }
        _fontRenderer.End();
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

    public static XnaFont LoadFont(GraphicsDevice device, string strXml, Texture2D tex, ContentManager content)
    {
        var fontLoader = new XmlSerializer(typeof(XnaFont));

        Stream reader = new FileStream(strXml, FileMode.Open);

        var ret = (XnaFont)fontLoader.Deserialize(reader);
        ret.WaterFall = content.Load<Texture2D>("Graphics\\waterfall");

        device.Textures[1] = ret.WaterFall;

        reader.Close();

        ret.RefractionEffect = content.Load<Effect>("Shaders\\refraction");
        ret.UvData = new Dictionary<char, XnaFontUv>();
        foreach (var uv in ret.UvList)
        {
            ret.UvData.Add(uv.C, uv);
        }

        ret.UvList = null;

        ret.Texture = tex;
        ret._fontRenderer = new SpriteBatch(device);

        return ret;
    }

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
}