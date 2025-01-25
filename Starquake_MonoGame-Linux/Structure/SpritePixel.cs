using Microsoft.Xna.Framework;
using StarquakeXNA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Starquake_MonoGame.Structure
{
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
        SpritePixel[] matrix;

        public SpritePixel[] Matrix
        {
            get { return matrix; }
        }

        public SpritePixelMatrix(int Size)
        {
            matrix = new SpritePixel[Size];

            for (int a = 0; a < matrix.Length; a++)
            {
                if (matrix[a] == null)
                {
                    matrix[a] = new SpritePixel() { IsOn = true };
                }
            }
        }

        public bool Finished
        {
            get
            {
                for (int a = 0; a < matrix.Length; a++)
                {
                    if (matrix[a].IsOn)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public void SetPixel(int Index, Color Colour)
        {
            matrix[Index].IsOn = false;
            matrix[Index].Colour = Colour;
        }

        public void SetRandomPixel(Color Colour)
        {
            if (!Finished)
            {
                SpritePixel p = null;

                do
                {
                    p = matrix[GameEngine.GetInstance().RandomNumberGenerator.Next(0, matrix.Length)];
                }
                while (p.Colour != Color.White);

                p.Colour = Colour;
                p.IsOn = false;
            }
        }
    }
}
