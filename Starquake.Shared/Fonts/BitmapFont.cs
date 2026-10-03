// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Helpers;

namespace Starquake.Fonts;

internal class BitmapFont
{
    private readonly SpriteFont _font;
    private readonly SpriteBatch _fontSpriteBatch;
    private Color _flashColour;
    private int _flashCounter;

    public BitmapFont(SpriteFont font, SpriteBatch fontSpriteBatch, Color colour1, Color colour2, int flashInterval)
    {
        _font = font;
        _fontSpriteBatch = fontSpriteBatch;
        FlashInterval = flashInterval;

        Colour1 = colour1;
        Colour2 = colour2;

        _flashColour = Colour1;
    }

    private Color Colour1 { get; }

    private Color Colour2 { get; }

    public bool Flash { get; set; }

    private int FlashInterval { get; }

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

    // Write some text with a predefined leading
    public void WriteText(int xPos, int yPos, string text, int leading, Color colour, byte screenFade)
    {
        for (int a = 0; a < text.Length; a++)
        {
            string letterToDraw = text.Substring(a, 1);
            Vector2 letterLength = MeasureString(letterToDraw);
            _fontSpriteBatch.DrawString(_font, letterToDraw, new Vector2(xPos, yPos), ColourHelper.GetXna4Colour(colour.R, colour.G, colour.B, screenFade));
            xPos = xPos + (int)letterLength.X + leading;
        }
    }

    public void WriteText(int xPos, int yPos, string text, int leading, Color colour, byte screenFade, float scale)
    {
        for (int a = 0; a < text.Length; a++)
        {
            string letterToDraw = text.Substring(a, 1);
            Vector2 letterLength = MeasureString(letterToDraw);
            _fontSpriteBatch.DrawString(_font, letterToDraw, new Vector2(xPos, yPos), ColourHelper.GetXna4Colour(colour.R, colour.G, colour.B, screenFade), 0, new Vector2(0, 0), scale, SpriteEffects.None, 0);
            xPos = xPos + (int)letterLength.X + leading;
        }
    }

    public void WriteText(int xPos, int yPos, string text, int leading, Color colour, byte screenFade, Vector2 scale, SpriteEffects spriteEffect)
    {
        for (int a = 0; a < text.Length; a++)
        {
            string letterToDraw = text.Substring(a, 1);
            Vector2 letterLength = MeasureString(letterToDraw);
            _fontSpriteBatch.DrawString(_font, letterToDraw, new Vector2(xPos, yPos), ColourHelper.GetXna4Colour(colour.R, colour.G, colour.B, screenFade), 0, new Vector2(0, 0), scale, spriteEffect, 0);
            xPos = xPos + (int)letterLength.X + leading;
        }
    }

    public void WriteTextCentred(int horizontalWidth, int yPos, string text, int leading, Color colour, byte screenFade)
    {
        Vector2 stringSize = MeasureString(text, leading);

        int indent = (horizontalWidth - (int)stringSize.X) / 2;

        WriteText(indent, yPos, text, leading, colour, screenFade);
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

    private Vector2 MeasureString(string text) => _font.MeasureString(text);

    // Measure the string and take into account horizontal leading
    private Vector2 MeasureString(string text, int leading)
    {
        Vector2 temp = _font.MeasureString(text);
        temp.X += (float)text.Length * leading;

        return temp;
    }
}
