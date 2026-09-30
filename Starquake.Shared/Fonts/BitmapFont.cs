// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Helpers;

namespace Starquake.Fonts;

public class BitmapFont : IDisposable
{
    private readonly SpriteFont _font;

    private Color _flashColour;

    private int _flashCounter;

    private SpriteBatch _fontSpriteBatch;

    public BitmapFont(SpriteFont font, SpriteBatch fontSpriteBatch)
    {
        _font = font;
        _fontSpriteBatch = fontSpriteBatch;
    }

    public BitmapFont(SpriteFont font, SpriteBatch fontSpriteBatch, Color colour1, Color colour2, int flashInterval)
    {
        _font = font;
        _fontSpriteBatch = fontSpriteBatch;
        this.FlashInterval = flashInterval;

        this.Colour1 = colour1;
        this.Colour2 = colour2;

        _flashColour = this.Colour1;
    }

    private Color Colour1 { get; }

    private Color Colour2 { get; }

    public bool Flash { get; set; }

    private int FlashInterval { get; }

    #region IDisposable Members

    public void Dispose()
    {
        if (_fontSpriteBatch != null)
        {
            _fontSpriteBatch.Dispose();
            _fontSpriteBatch = null;
        }
    }

    #endregion

    public void Update()
    {
        if (Flash)
        {
            if (_flashCounter > FlashInterval)
            {
                _flashCounter = 0;

                if (_flashColour == Colour1)
                {
                    _flashColour = Colour2;
                }
                else if (_flashColour == Colour2)
                {
                    _flashColour = Colour1;
                }
            }

            _flashCounter++;
        }
    }

    public void Begin(BlendState blendState)
    {
        _fontSpriteBatch.Begin(SpriteSortMode.Deferred, blendState);
    }

    public void WriteText(int xPos, int yPos, string text, Color colour, byte screenFade)
    {
        _fontSpriteBatch.DrawString(_font, text, new Vector2(xPos, yPos), ColourHelper.GetXna4Colour(colour.R, colour.G, colour.B, screenFade));
    }

    // Write spome text with a predefined leading
    public void WriteText(int xPos, int yPos, string text, int leading, Color colour, byte screenFade)
    {
        for (var a = 0; a < text.Length; a++)
        {
            var letterToDraw = text.Substring(a, 1);
            var letterLength = MeasureString(letterToDraw);
            _fontSpriteBatch.DrawString(_font, letterToDraw, new Vector2(xPos, yPos), ColourHelper.GetXna4Colour(colour.R, colour.G, colour.B, screenFade));
            xPos = xPos + (int)letterLength.X + leading;
        }
    }

    public void WriteText(int xPos, int yPos, string text, int leading, Color colour, byte screenFade, float scale)
    {
        for (var a = 0; a < text.Length; a++)
        {
            var letterToDraw = text.Substring(a, 1);
            var letterLength = MeasureString(letterToDraw);
            _fontSpriteBatch.DrawString(_font, letterToDraw, new Vector2(xPos, yPos), ColourHelper.GetXna4Colour(colour.R, colour.G, colour.B, screenFade), 0, new Vector2(0, 0), scale, SpriteEffects.None, 0);
            xPos = xPos + (int)letterLength.X + leading;
        }
    }

    public void WriteText(int xPos, int yPos, string text, int leading, Color colour, byte screenFade, Vector2 scale, SpriteEffects spriteEffect)
    {
        for (var a = 0; a < text.Length; a++)
        {
            var letterToDraw = text.Substring(a, 1);
            var letterLength = MeasureString(letterToDraw);
            _fontSpriteBatch.DrawString(_font, letterToDraw, new Vector2(xPos, yPos), ColourHelper.GetXna4Colour(colour.R, colour.G, colour.B, screenFade), 0, new Vector2(0, 0), scale, spriteEffect, 0);
            xPos = xPos + (int)letterLength.X + leading;
        }
    }

    public void WriteTextCentred(int horizontalWidth, int yPos, string text, int leading, Color colour, byte screenFade)
    {
        var stringSize = MeasureString(text, leading);

        var indent = (horizontalWidth - (int)stringSize.X) / 2;

        WriteText(indent, yPos, text, leading, colour, screenFade);
    }

    public void WriteTextAltColours(int xPos, int yPos, string text, Color colour1, Color colour2, byte screenFade)
    {
        for (var a = 0; a < text.Length; a++)
        {
            var letterToDraw = text.Substring(a, 1);
            var letterLength = MeasureString(letterToDraw);

            if (a % 2 == 0)
            {
                _fontSpriteBatch.DrawString(_font, letterToDraw, new Vector2(xPos, yPos), ColourHelper.GetXna4Colour(colour1.R, colour1.G, colour1.B, screenFade));
            }
            else
            {
                _fontSpriteBatch.DrawString(_font, letterToDraw, new Vector2(xPos, yPos), ColourHelper.GetXna4Colour(colour2.R, colour2.G, colour2.B, screenFade));
            }

            xPos = xPos + (int)letterLength.X;
        }
    }

    public void WriteText(int xPos, int yPos, string text, byte screenFade)
    {
        if (!Flash)
        {
            _fontSpriteBatch.DrawString(_font, text, new Vector2(xPos, yPos), ColourHelper.GetXna4Colour(Colour1.R, Colour1.G, Colour1.B, screenFade));
        }
        else
        {
            _fontSpriteBatch.DrawString(_font, text, new Vector2(xPos, yPos), ColourHelper.GetXna4Colour(_flashColour.R, _flashColour.G, _flashColour.B, screenFade));
        }
    }

    public void WriteText(int xPos, int yPos, string text, int leading, byte screenFade)
    {
        if (!Flash)
        {
            WriteText(xPos, yPos, text, leading, Colour1, screenFade);
        }
        else
        {
            WriteText(xPos, yPos, text, leading, _flashColour, screenFade);
        }
    }

    public void End()
    {
        _fontSpriteBatch.End();
    }

    private Vector2 MeasureString(string text)
        => _font.MeasureString(text);

    // Measure the string and take into account horizontal leading
    private Vector2 MeasureString(string text, int leading)
    {
        var temp = _font.MeasureString(text);
        temp.X = temp.X + (float)text.Length * leading;

        return temp;
    }
}