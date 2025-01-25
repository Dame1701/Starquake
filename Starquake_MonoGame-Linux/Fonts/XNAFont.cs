using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System.Xml;
using System.IO;
using System.Xml.Serialization;

namespace StarquakeXNA
{
    [Serializable]
    [XmlRoot()]
    public class XNAFont : IDisposable
    {
        [XmlIgnore()]
        public Texture2D Texture;

        public Texture2D waterFall;

        [XmlIgnore()]
        SpriteBatch fontRenderer;

        string fontFamily;
        public string FontFamily
        {
            get { return fontFamily; }
            set { fontFamily = value; }
        }

        int height;
        public int Height
        {
            get { return height; }
            set { height = value; }
        }

        int kerning = 0;
        public int Kerning
        {
            get { return kerning; }
            set { kerning = value; }
        }

        [Serializable()]
        public class XNAFontUV
        {
            [XmlAttribute()]
            public int U;

            [XmlAttribute()]
            public int V;

            [XmlAttribute()]
            public int Width;

            [XmlAttribute()]
            public char C;

            public XNAFontUV()
            {
                U = 0;
                V = 0;
                Width = 0;
                C = (char)0;
            }

            public XNAFontUV(int u, int v, char c, int Width)
            {
                this.U = u;
                this.V = v;
                this.C = c;
                this.Width = Width;
            }
        }

        [XmlIgnore()]
        public Dictionary<char, XNAFontUV> UVData = new Dictionary<char, XNAFontUV>();

        public List<XNAFontUV> UVList;

        [XmlIgnore()]
        public Effect refractionEffect;

        public void OutputText(string str, int x, int y, Color col)
        {
            OutputText(str, x, y, col, null);
        }

        public void OutputText(string str, int x, int y, Color col, GameTime gameTime)
        {
            int iCurrentX = x;
            int iCurrentY = y;

            // Draw the sprite.
            fontRenderer.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

            if (gameTime != null)
            {
                refractionEffect.Parameters["DisplacementScroll"].SetValue(MoveInCircle(gameTime, 0.1f));

                // Begin the custom effect.
                refractionEffect.CurrentTechnique.Passes[0].Apply();
            }

            
            foreach (char c in str)
            {
                XNAFontUV uv = UVData[c];

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
        /// Helper for moving a value around in a circle.
        /// </summary>
        static Vector2 MoveInCircle(GameTime gameTime, float speed)
        {
            double time = gameTime.TotalGameTime.TotalSeconds * speed;

            float x = (float)Math.Cos(time);
            float y = (float)Math.Sin(time);

            return new Vector2(x, y);
        }

        public static XNAFont LoadFont(GraphicsDevice device, string strXML, Texture2D tex, ContentManager Content)
        {
            XmlSerializer fontLoader = new XmlSerializer(typeof(XNAFont));

            Stream reader = new FileStream(strXML, FileMode.Open);

            XNAFont ret = (XNAFont)fontLoader.Deserialize(reader);
            ret.waterFall = Content.Load<Texture2D>("Graphics\\waterfall");

            device.Textures[1] = ret.waterFall;

            reader.Close();

            ret.refractionEffect = Content.Load<Effect>("Shaders\\refraction");
            ret.UVData = new Dictionary<char, XNAFontUV>();
            foreach (XNAFontUV uv in ret.UVList)
            {
                ret.UVData.Add(uv.C, uv);
            }

            ret.UVList = null;

            ret.Texture = tex;
            ret.fontRenderer = new SpriteBatch(device);

            return ret;
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
    }
    

}
