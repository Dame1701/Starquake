// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

/*
 Copyright © 2009 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ProjectMercury.Emitters;

/// <summary>
///     Obsolete.
/// </summary>
#if WINDOWS
    [TypeConverter("ProjectMercury.Design.Emitters.TextureEmitterTypeConverter, ProjectMercury.Design")]
#endif
[Obsolete("Replaced by MaskEmitter")]
public class TextureEmitter : Emitter
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TextureEmitter" /> class.
    /// </summary>
    public TextureEmitter()
    {
        Scale = 1f;
        PixelOffsets = new Vector2[0];
        Threshold = 0.5f;
    }

    private Matrix ScaleMatrix;

    /// <summary>
    ///     Gets or sets the scale factor of the texture (in screen space).
    /// </summary>
    public float Scale
    {
        get => ScaleMatrix.M11;
        set => ScaleMatrix = Matrix.CreateScale(value);
    }

    private Vector2 TextureOrigin;

    private Vector2[] PixelOffsets;

    private Vector3[] PixelColours;

    private Texture2D _texture;

    /// <summary>
    ///     Gets or sets the texture used to lookup particle release offsets.
    /// </summary>
    /// <value>The texture.</value>
    public Texture2D Texture
    {
        get => _texture;
        set
        {
            Guard.ArgumentNull("Texture", value);

            if (Texture != value)
            {
                _texture = value;

                CalculateEmissionPoints();
            }
        }
    }

    /// <summary>
    ///     Gets or sets a value indicating whether particles should assume the colour of the underlying
    ///     pixel in the texture
    /// </summary>
    public bool ApplyPixelColours { get; set; }

    /// <summary>
    ///     Gets or sets the threshold over which pixels will trigger the release of particles.
    /// </summary>
    public float Threshold;

    /// <summary>
    ///     Calculates the emission points.
    /// </summary>
    private void CalculateEmissionPoints()
    {
        if (Texture == null)
        {
            TextureOrigin = Vector2.Zero;

            Array.Resize(ref PixelOffsets, 0);
            Array.Resize(ref PixelColours, 0);

            return;
        }

        TextureOrigin = new Vector2(Texture.Width / 2, Texture.Height / 2);

        var offsets = new List<Vector2>();
        var colours = new List<Vector3>();

        var pixels = new Color[Texture.Width * Texture.Height];

        Texture.GetData(pixels);

        int sourceIndex, destIndex = 0;

        byte minOpacity = Convert.ToByte(Threshold * 255f);

        for (int x = 0; x < Texture.Width; x++)
        {
            for (int y = 0; y < Texture.Height; y++)
            {
                sourceIndex = Texture.Width * y + x;

                if (pixels[sourceIndex].A >= minOpacity)
                {
                    offsets.Add(new Vector2
                    {
                        X = x - TextureOrigin.X,
                        Y = y - TextureOrigin.Y
                    });

                    colours.Add(pixels[sourceIndex].ToVector3());

                    destIndex++;
                }
            }
        }

        PixelOffsets = offsets.ToArray();
        PixelColours = colours.ToArray();
    }

    /// <summary>
    ///     Returns an uninitialised deep copy of the Emitter.
    /// </summary>
    /// <returns>A deep copy of the Emitter.</returns>
    public override Emitter DeepCopy()
    {
        Emitter copy = new TextureEmitter
        {
            ApplyPixelColours = ApplyPixelColours,
            Scale = Scale,
            Texture = Texture,
            Threshold = Threshold
        };

        CopyBaseFields(copy);

        return copy;
    }

    /// <summary>
    ///     Generates an offset vector and force vector for a Particle when it is released.
    /// </summary>
    /// <param name="offset">The offset of the Particle from the trigger location.</param>
    /// <param name="force">A unit vector defining the initial force of the Particle.</param>
    protected override void GenerateOffsetAndForce(out Vector2 offset, out Vector2 force)
    {
        int index = RandomHelper.NextInt(PixelOffsets.Length);

        offset = PixelOffsets[index];

        offset.X *= Scale;
        offset.Y *= Scale;

        if (ApplyPixelColours)
        {
            ReleaseColour = PixelColours[index];
        }

        force = RandomHelper.NextUnitVector();
    }
}