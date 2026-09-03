using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using StarquakeXNA.Helpers;

namespace StarquakeXNA
{
    public class BitmapFont : IDisposable
    {
        private SpriteFont font;
        private SpriteBatch fontSpriteBatch;

        private Color colour1;
        public Color Colour1
        {
            get { return colour1; }
            set { colour1 = value; }
        }

        private Color colour2;
        public Color Colour2
        {
            get { return colour2; }
            set { colour2 = value; }
        }

        private Color flashColour;

        private bool flash;
        public bool Flash
        {
            get { return flash; }
            set { flash = value; }
        }

        private int flashInterval;
        public int FlashInterval
        {
            get { return flashInterval; }
            set { flashInterval = value; }
        }

        private int flashCounter = 0;

        public BitmapFont(SpriteFont Font, SpriteBatch FontSpriteBatch)
        {
            this.font = Font;
            this.fontSpriteBatch = FontSpriteBatch;
        }

        public BitmapFont(SpriteFont Font, SpriteBatch FontSpriteBatch, Color Colour1, Color Colour2, int FlashInterval)
        {
            this.font = Font;
            this.fontSpriteBatch = FontSpriteBatch;
            this.flashInterval = FlashInterval;

            this.colour1 = Colour1;
            this.colour2 = Colour2;

            this.flashColour = this.colour1;
        }

        public void Update()
        {
            if (flash)
            {
                if (flashCounter > flashInterval)
                {
                    flashCounter = 0;

                    if (flashColour == colour1)
                    {
                        flashColour = colour2;
                    }
                    else if (flashColour == colour2)
                    {
                        flashColour = colour1;
                    }
                }

                flashCounter++;
            }
        }

        public void Begin(BlendState BlendState)
        {
            fontSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState);
        }

        public void WriteText(int XPos, int YPos, string Text, Color Colour, byte ScreenFade)
        {
            fontSpriteBatch.DrawString(font, Text, new Vector2(XPos, YPos),  ColourHelper.GetXNA4Colour(Colour.R, Colour.G, Colour.B, ScreenFade)); 
        }

        // Write spome text with a predefined leading
        public void WriteText(int XPos, int YPos, string Text, int Leading, Color Colour, byte ScreenFade)
        {
            for (int a = 0; a < Text.Length; a++)
            {
                string letterToDraw = Text.Substring(a, 1);
                Vector2 letterLength = this.MeasureString(letterToDraw);
                fontSpriteBatch.DrawString(font, letterToDraw, new Vector2(XPos, YPos), ColourHelper.GetXNA4Colour(Colour.R, Colour.G, Colour.B, ScreenFade));
                XPos = XPos + (int)letterLength.X + Leading;
            }
        }

        public void WriteText(int XPos, int YPos, string Text, int Leading, Color Colour, byte ScreenFade, float Scale)
        {
            for (int a = 0; a < Text.Length; a++)
            {
                string letterToDraw = Text.Substring(a, 1);
                Vector2 letterLength = this.MeasureString(letterToDraw);
                fontSpriteBatch.DrawString(font, letterToDraw, new Vector2(XPos, YPos), ColourHelper.GetXNA4Colour(Colour.R, Colour.G, Colour.B, ScreenFade), 0, new Vector2(0, 0), Scale, SpriteEffects.None, 0);
                XPos = XPos + (int)letterLength.X + Leading;
            }
        }

        public void WriteText(int XPos, int YPos, string Text, int Leading, Color Colour, byte ScreenFade, Vector2 Scale, SpriteEffects SpriteEffect)
        {
            for (int a = 0; a < Text.Length; a++)
            {
                string letterToDraw = Text.Substring(a, 1);
                Vector2 letterLength = this.MeasureString(letterToDraw);
                fontSpriteBatch.DrawString(font, letterToDraw, new Vector2(XPos, YPos), ColourHelper.GetXNA4Colour(Colour.R, Colour.G, Colour.B, ScreenFade), 0, new Vector2(0, 0), Scale, SpriteEffect, 0);
                XPos = XPos + (int)letterLength.X + Leading;
            }
        }

        public void WriteTextCentred(int HorizontalWidth, int YPos, string Text, int Leading, Color Colour, byte ScreenFade)
        {
            Vector2 stringSize = MeasureString(Text, Leading);

            int indent = (HorizontalWidth - (int)stringSize.X) / 2;

            WriteText(indent, YPos, Text, Leading, Colour, ScreenFade);
        }

        public void WriteTextAltColours(int XPos, int YPos, string Text, Color Colour1, Color Colour2, byte ScreenFade)
        {
            for (int a = 0; a < Text.Length; a++)
            {
                string letterToDraw = Text.Substring(a, 1);
                Vector2 letterLength = this.MeasureString(letterToDraw);

                if (a % 2 == 0)
                {
                    fontSpriteBatch.DrawString(font, letterToDraw, new Vector2(XPos, YPos), ColourHelper.GetXNA4Colour(Colour1.R, Colour1.G, Colour1.B, ScreenFade));
                }
                else
                {
                    fontSpriteBatch.DrawString(font, letterToDraw, new Vector2(XPos, YPos), ColourHelper.GetXNA4Colour(Colour2.R, Colour2.G, Colour2.B, ScreenFade));
                }

                XPos = XPos + (int)letterLength.X;
            }
        }

        public void WriteText(int XPos, int YPos, string Text, byte ScreenFade)
        {
            if (!flash)
            {
                fontSpriteBatch.DrawString(font, Text, new Vector2(XPos, YPos),  ColourHelper.GetXNA4Colour(Colour1.R, Colour1.G, Colour1.B, ScreenFade));
            }
            else
            {
                fontSpriteBatch.DrawString(font, Text, new Vector2(XPos, YPos),  ColourHelper.GetXNA4Colour(flashColour.R, flashColour.G, flashColour.B, ScreenFade));
            }
        }

        public void WriteText(int XPos, int YPos, string Text, int Leading, byte ScreenFade)
        {
            if (!flash)
            {
                WriteText(XPos, YPos, Text, Leading, Colour1, ScreenFade);
            }
            else
            {
                WriteText(XPos, YPos, Text, Leading, flashColour, ScreenFade);
            }
        }

        public void End()
        {
            fontSpriteBatch.End();
        }

        public Vector2 MeasureString(string Text)
        {
            return font.MeasureString(Text);
        }

        // Measure the string and take into account horizontal leading
        public Vector2 MeasureString(string Text, int Leading)
        {
            Vector2 temp = font.MeasureString(Text);
            temp.X = temp.X + (float)Text.Length * Leading;

            return temp;
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (fontSpriteBatch != null)
            {
                fontSpriteBatch.Dispose();
                fontSpriteBatch = null;
            }
        }

        #endregion
    }
}
