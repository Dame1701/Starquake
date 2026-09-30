// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Engines;

namespace Starquake.Sprites;

public class SpritePixelMatrix
{

    public SpritePixelMatrix(int size)
    {
        Matrix = new SpritePixel[size];

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
            foreach (var matrix in Matrix)
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
            SpritePixel p = null;

            do
            {
                p = Matrix[GameEngine.GetInstance().RandomNumberGenerator.Next(0, Matrix.Length)];
            } while (p.Colour != Color.White);

            p.Colour = colour;
            p.IsOn = false;
        }
    }
}