// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;

namespace Starquake.Sprites;

internal class SpritePixelMatrix
{
    private readonly Random _randomNumberGenerator;

    public SpritePixelMatrix(int size, Random randomNumberGenerator)
    {
        _randomNumberGenerator = randomNumberGenerator;

        // The array must be filled, not just allocated. Every pixel starts on and white, which is
        // both what Finished counts down from and what SetRandomPixel's loop looks for in an
        // untouched pixel.
        Matrix = new SpritePixel[size];
        for (int a = 0; a < Matrix.Length; a++)
        {
            Matrix[a] = new SpritePixel();
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

    public void SetRandomPixel(Color colour)
    {
        if (Finished)
        {
            return;
        }

        SpritePixel p;

        do
        {
            p = Matrix[_randomNumberGenerator.Next(0, Matrix.Length)];
        } while (p.Colour != Color.White);

        p.Colour = colour;
        p.IsOn = false;
    }
}
