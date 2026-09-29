// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Engines;
using Starquake.Sprites;

namespace Starquake.Fonts;

public class ImageFont : IDisposable
{
    private readonly Dictionary<string, int> _letterMap;
    private SpriteCollection _imageFont;

    public ImageFont(GameEngine gameEngineRef, ContentManager content)
    {
        _letterMap = new Dictionary<string, int>();
        _letterMap.Add("A", 1);
        _letterMap.Add("B", 2);
        _letterMap.Add("C", 3);
        _letterMap.Add("D", 4);
        _letterMap.Add("E", 5);
        _letterMap.Add("F", 6);
        _letterMap.Add("G", 7);
        _letterMap.Add("H", 8);
        _letterMap.Add("I", 9);
        _letterMap.Add("J", 10);
        _letterMap.Add("K", 11);
        _letterMap.Add("L", 12);
        _letterMap.Add("M", 13);
        _letterMap.Add("N", 14);
        _letterMap.Add("O", 15);
        _letterMap.Add("P", 16);
        _letterMap.Add("Q", 17);
        _letterMap.Add("R", 18);
        _letterMap.Add("S", 19);
        _letterMap.Add("T", 20);
        _letterMap.Add("U", 21);
        _letterMap.Add("V", 22);
        _letterMap.Add("W", 23);
        _letterMap.Add("X", 24);
        _letterMap.Add("Y", 25);
        _letterMap.Add("Z", 26);
        _letterMap.Add("0", 27);
        _letterMap.Add("1", 28);
        _letterMap.Add("2", 29);
        _letterMap.Add("3", 30);
        _letterMap.Add("4", 31);
        _letterMap.Add("5", 32);
        _letterMap.Add("6", 33);
        _letterMap.Add("7", 34);
        _letterMap.Add("8", 35);
        _letterMap.Add("9", 36);

        _imageFont = new SpriteCollection(gameEngineRef, content, "Graphics\\StarFont", 32, 32);

    }

    #region IDisposable Members

    public void Dispose()
    {
        if (_imageFont != null)
        {
            _imageFont = null;
        }
    }

    #endregion

    public void WriteText(string text, int xLocation, int yLocation, GameTime time, Color textColour)
    {
        if (_imageFont != null)
        {
            _imageFont.Begin(BlendState.AlphaBlend, time);
            for (var a = 0; a < text.Length; a++)
            {
                var imageNo = 0;
                _letterMap.TryGetValue(text.Substring(a, 1).ToUpper(), out imageNo);

                if (imageNo > 0)
                {
                    _imageFont.DrawSprite(imageNo - 1, xLocation + a * 36, yLocation, textColour);
                }
            }

            _imageFont.End();
        }
    }
}