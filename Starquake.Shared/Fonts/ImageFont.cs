// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Structure;

namespace StarquakeXNA;

public class ImageFont : IDisposable
{
    private readonly Dictionary<string, int> letterMap;
    private SpriteCollection imageFont;

    public ImageFont(GameEngine GameEngineRef, ContentManager Content)
    {
        letterMap = new Dictionary<string, int>();
        letterMap.Add("A", 1);
        letterMap.Add("B", 2);
        letterMap.Add("C", 3);
        letterMap.Add("D", 4);
        letterMap.Add("E", 5);
        letterMap.Add("F", 6);
        letterMap.Add("G", 7);
        letterMap.Add("H", 8);
        letterMap.Add("I", 9);
        letterMap.Add("J", 10);
        letterMap.Add("K", 11);
        letterMap.Add("L", 12);
        letterMap.Add("M", 13);
        letterMap.Add("N", 14);
        letterMap.Add("O", 15);
        letterMap.Add("P", 16);
        letterMap.Add("Q", 17);
        letterMap.Add("R", 18);
        letterMap.Add("S", 19);
        letterMap.Add("T", 20);
        letterMap.Add("U", 21);
        letterMap.Add("V", 22);
        letterMap.Add("W", 23);
        letterMap.Add("X", 24);
        letterMap.Add("Y", 25);
        letterMap.Add("Z", 26);
        letterMap.Add("0", 27);
        letterMap.Add("1", 28);
        letterMap.Add("2", 29);
        letterMap.Add("3", 30);
        letterMap.Add("4", 31);
        letterMap.Add("5", 32);
        letterMap.Add("6", 33);
        letterMap.Add("7", 34);
        letterMap.Add("8", 35);
        letterMap.Add("9", 36);

        imageFont = new SpriteCollection(GameEngineRef, Content, "Graphics\\StarFont", 32, 32);

    }

    #region IDisposable Members

    public void Dispose()
    {
        if (imageFont != null)
        {
            imageFont = null;
        }
    }

    #endregion

    public void WriteText(string Text, int XLocation, int YLocation, GameTime Time, Color TextColour)
    {
        if (imageFont != null)
        {
            imageFont.Begin(BlendState.AlphaBlend, Time);
            for (var a = 0; a < Text.Length; a++)
            {
                var imageNo = 0;
                letterMap.TryGetValue(Text.Substring(a, 1).ToUpper(), out imageNo);

                if (imageNo > 0)
                {
                    imageFont.DrawSprite(imageNo - 1, XLocation + a * 36, YLocation, TextColour);
                }
            }

            imageFont.End();
        }
    }
}