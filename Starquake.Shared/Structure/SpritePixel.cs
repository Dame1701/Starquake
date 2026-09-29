// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using StarquakeXNA;

namespace Starquake_MonoGame.Structure;

public class SpritePixel
{
    public SpritePixel()
    {
        IsOn = true;
        Colour = Color.White;
    }
    public bool IsOn { get; set; }

    public Color Colour { get; set; }
}

public class SpritePixelMatrix
{

    public SpritePixelMatrix(int Size)
    {
        Matrix = new SpritePixel[Size];

        for (var a = 0; a < Matrix.Length; a++)
        {
            if (Matrix[a] == null)
            {
                Matrix[a] = new SpritePixel
                {
                    IsOn = true
                };
            }
        }
    }

    public SpritePixel[] Matrix { get; }

    public bool Finished
    {
        get
        {
            for (var a = 0; a < Matrix.Length; a++)
            {
                if (Matrix[a].IsOn)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public void SetPixel(int Index, Color Colour)
    {
        Matrix[Index].IsOn = false;
        Matrix[Index].Colour = Colour;
    }

    public void SetRandomPixel(Color Colour)
    {
        if (!Finished)
        {
            SpritePixel p = null;

            do
            {
                p = Matrix[GameEngine.GetInstance().RandomNumberGenerator.Next(0, Matrix.Length)];
            } while (p.Colour != Color.White);

            p.Colour = Colour;
            p.IsOn = false;
        }
    }
}