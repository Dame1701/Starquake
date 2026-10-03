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

        // A font reserves room above its capitals for accents and below them for descenders, so the
        // line it draws into is taller than the letters in it. Text that has to land inside artwork
        // is lined up on the capitals, which is why these come off a capital's own glyph rather than
        // off the line height. A font with no A to measure falls back to the line, which is wrong by
        // a few pixels rather than not drawn at all.
        if (font.GetGlyphs().TryGetValue('A', out SpriteFont.Glyph capital))
        {
            CapitalTop = capital.Cropping.Y;
            CapitalHeight = capital.BoundsInTexture.Height;
        }
        else
        {
            CapitalHeight = (int)font.MeasureString("A").Y;
        }
    }

    /// <summary>
    ///     How far below the top of a line of this font a capital letter starts, and how tall one is
    ///     drawn, both in pixels at a scale of one. Fitting text to artwork means fitting its
    ///     capitals, and where they sit in the line is a property of the typeface rather than of
    ///     anything on the screen.
    /// </summary>
    public int CapitalTop { get; }

    /// <inheritdoc cref="CapitalTop" />
    public int CapitalHeight { get; }

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

    /// <summary>
    ///     Writes a string at a scale, letting the font space its own letters.
    /// </summary>
    /// <remarks>
    ///     The <c>leading</c> overloads above draw a letter at a time so that the gap between letters
    ///     can be widened, and the one that takes a scale steps on by each letter's unscaled width -
    ///     which is what the teleporter's drop shadow is offset by, so it cannot be put right there.
    ///     Text that has to land inside artwork needs neither: its width has to be exactly
    ///     <see cref="MeasureString(string)" /> times the scale for it to be centred on anything.
    /// </remarks>
    public void WriteScaledText(int xPos, int yPos, string text, Color colour, byte screenFade, float scale)
    {
        _fontSpriteBatch.DrawString(
            _font,
            text,
            new Vector2(xPos, yPos),
            ColourHelper.GetXna4Colour(colour.R, colour.G, colour.B, screenFade),
            0,
            new Vector2(0, 0),
            scale,
            SpriteEffects.None,
            0);
    }

    public void End()
    {
        _fontSpriteBatch.End();
    }

    /// <summary>
    ///     How wide and tall a string will be once drawn, at a scale of one.
    /// </summary>
    public Vector2 MeasureString(string text) => _font.MeasureString(text);

    /// <summary>
    ///     How wide and tall a string will be once drawn, with the gap between characters counted in.
    /// </summary>
    private Vector2 MeasureString(string text, int leading)
    {
        Vector2 temp = _font.MeasureString(text);
        temp.X += (float)text.Length * leading;

        return temp;
    }
}
