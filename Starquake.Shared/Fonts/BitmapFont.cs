// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Helpers;

namespace StarquakeXNA;

public class BitmapFont : IDisposable
{
    private readonly SpriteFont font;

    private Color flashColour;

    private int flashCounter;

    private SpriteBatch fontSpriteBatch;

    public BitmapFont(SpriteFont Font, SpriteBatch FontSpriteBatch)
    {
        font = Font;
        fontSpriteBatch = FontSpriteBatch;
    }

    public BitmapFont(SpriteFont Font, SpriteBatch FontSpriteBatch, Color Colour1, Color Colour2, int FlashInterval)
    {
        font = Font;
        fontSpriteBatch = FontSpriteBatch;
        this.FlashInterval = FlashInterval;

        this.Colour1 = Colour1;
        this.Colour2 = Colour2;

        flashColour = this.Colour1;
    }

    public Color Colour1 { get; set; }

    public Color Colour2 { get; set; }

    public bool Flash { get; set; }

    public int FlashInterval { get; set; }

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

    public void Update()
    {
        if (Flash)
        {
            if (flashCounter > FlashInterval)
            {
                flashCounter = 0;

                if (flashColour == Colour1)
                {
                    flashColour = Colour2;
                }
                else if (flashColour == Colour2)
                {
                    flashColour = Colour1;
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
        fontSpriteBatch.DrawString(font, Text, new Vector2(XPos, YPos), ColourHelper.GetXNA4Colour(Colour.R, Colour.G, Colour.B, ScreenFade));
    }

    // Write spome text with a predefined leading
    public void WriteText(int XPos, int YPos, string Text, int Leading, Color Colour, byte ScreenFade)
    {
        for (var a = 0; a < Text.Length; a++)
        {
            var letterToDraw = Text.Substring(a, 1);
            var letterLength = MeasureString(letterToDraw);
            fontSpriteBatch.DrawString(font, letterToDraw, new Vector2(XPos, YPos), ColourHelper.GetXNA4Colour(Colour.R, Colour.G, Colour.B, ScreenFade));
            XPos = XPos + (int)letterLength.X + Leading;
        }
    }

    public void WriteText(int XPos, int YPos, string Text, int Leading, Color Colour, byte ScreenFade, float Scale)
    {
        for (var a = 0; a < Text.Length; a++)
        {
            var letterToDraw = Text.Substring(a, 1);
            var letterLength = MeasureString(letterToDraw);
            fontSpriteBatch.DrawString(font, letterToDraw, new Vector2(XPos, YPos), ColourHelper.GetXNA4Colour(Colour.R, Colour.G, Colour.B, ScreenFade), 0, new Vector2(0, 0), Scale, SpriteEffects.None, 0);
            XPos = XPos + (int)letterLength.X + Leading;
        }
    }

    public void WriteText(int XPos, int YPos, string Text, int Leading, Color Colour, byte ScreenFade, Vector2 Scale, SpriteEffects SpriteEffect)
    {
        for (var a = 0; a < Text.Length; a++)
        {
            var letterToDraw = Text.Substring(a, 1);
            var letterLength = MeasureString(letterToDraw);
            fontSpriteBatch.DrawString(font, letterToDraw, new Vector2(XPos, YPos), ColourHelper.GetXNA4Colour(Colour.R, Colour.G, Colour.B, ScreenFade), 0, new Vector2(0, 0), Scale, SpriteEffect, 0);
            XPos = XPos + (int)letterLength.X + Leading;
        }
    }

    public void WriteTextCentred(int HorizontalWidth, int YPos, string Text, int Leading, Color Colour, byte ScreenFade)
    {
        var stringSize = MeasureString(Text, Leading);

        var indent = (HorizontalWidth - (int)stringSize.X) / 2;

        WriteText(indent, YPos, Text, Leading, Colour, ScreenFade);
    }

    public void WriteTextAltColours(int XPos, int YPos, string Text, Color Colour1, Color Colour2, byte ScreenFade)
    {
        for (var a = 0; a < Text.Length; a++)
        {
            var letterToDraw = Text.Substring(a, 1);
            var letterLength = MeasureString(letterToDraw);

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
        if (!Flash)
        {
            fontSpriteBatch.DrawString(font, Text, new Vector2(XPos, YPos), ColourHelper.GetXNA4Colour(Colour1.R, Colour1.G, Colour1.B, ScreenFade));
        }
        else
        {
            fontSpriteBatch.DrawString(font, Text, new Vector2(XPos, YPos), ColourHelper.GetXNA4Colour(flashColour.R, flashColour.G, flashColour.B, ScreenFade));
        }
    }

    public void WriteText(int XPos, int YPos, string Text, int Leading, byte ScreenFade)
    {
        if (!Flash)
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
        => font.MeasureString(Text);

    // Measure the string and take into account horizontal leading
    public Vector2 MeasureString(string Text, int Leading)
    {
        var temp = font.MeasureString(Text);
        temp.X = temp.X + (float)Text.Length * Leading;

        return temp;
    }
}