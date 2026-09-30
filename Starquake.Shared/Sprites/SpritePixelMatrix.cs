// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework;

namespace Starquake.Sprites;

public class SpritePixelMatrix
{
    private readonly Random _randomNumberGenerator;

    public SpritePixelMatrix(int size, Random randomNumberGenerator)
    {
        _randomNumberGenerator = randomNumberGenerator;

        Matrix = new SpritePixel[size];

        for (int a = 0; a < Matrix.Length; a++)
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
            foreach (SpritePixel matrix in Matrix)
            {
                if (matrix.IsOn)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public void SetPixel(int index, Color colour)
    {
        Matrix[index].IsOn = false;
        Matrix[index].Colour = colour;
    }

    public void SetRandomPixel(Color colour)
    {
        if (!Finished)
        {
            SpritePixel p;

            do
            {
                p = Matrix[_randomNumberGenerator.Next(0, Matrix.Length)];
            } while (p.Colour != Color.White);

            p.Colour = colour;
            p.IsOn = false;
        }
    }
}
