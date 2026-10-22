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

namespace ProjectMercury.Modifiers;

/// <summary>
///     Defines a modifier which changes the colour of particles based on a linear interpolation over three values.
/// </summary>
#if WINDOWS
    [TypeConverter("ProjectMercury.Design.Modifiers.ColourInterpolatorModifierTypeConverter, ProjectMercury.Design")]
#endif
public class ColourInterpolatorModifier : Modifier
{
    /// <summary>
    ///     Gets or sets the initial colour.
    /// </summary>
    /// <value>The initial colour.</value>
    public Vector3 InitialColour { get; set; }

    /// <summary>
    ///     Gets or sets the middle colour.
    /// </summary>
    /// <value>The middle colour.</value>
    public Vector3 MiddleColour { get; set; }

    private float _middlePosition;

    /// <summary>
    ///     Gets or sets the middle colour position.
    /// </summary>
    /// <value>The middle position.</value>
    public float MiddlePosition
    {
        get => _middlePosition;
        set
        {
            Guard.ArgumentOutOfRange("MiddlePosition", value, 0f, 1f);

            _middlePosition = value;
        }
    }

    /// <summary>
    ///     Gets or sets the final colour.
    /// </summary>
    /// <value>The final colour.</value>
    public Vector3 FinalColour { get; set; }

    /// <summary>
    ///     Returns a deep copy of the Modifier implementation.
    /// </summary>
    /// <returns></returns>
    public override Modifier DeepCopy()
        => new ColourInterpolatorModifier
        {
            FinalColour = FinalColour,
            InitialColour = InitialColour,
            MiddleColour = MiddleColour,
            MiddlePosition = MiddlePosition
        };

    /// <summary>
    ///     Processes the particles.
    /// </summary>
    /// <param name="elapsedSeconds">Elapsed time in whole and fractional seconds.</param>
    /// <param name="particle">A pointer to an array of particles.</param>
    /// <param name="count">The number of particles which need to be processed.</param>
    internal protected override unsafe void Process(float elapsedSeconds, Particle* particle, int count)
    {
        float lerpPosition;
        Vector3 lerpColourA;
        Vector3 lerpColourB;

        for (int i = 0; i < count; i++)
        {
            Particle* previousParticle = particle - 1;

            // If the particle was released at the same time as the previous particle, there is no need to
            // interpolate the colour, so just copy it from the previous particle...
            if (particle->Age == previousParticle->Age)
            {
                particle->Colour.X = previousParticle->Colour.X;
                particle->Colour.Y = previousParticle->Colour.Y;
                particle->Colour.Z = previousParticle->Colour.Z;
            }
            else
            {
                // If the particle age is less than middle position, interpolate between InitialColour and MiddleColour...
                if (particle->Age < MiddlePosition)
                {
                    lerpColourA = InitialColour;
                    lerpColourB = MiddleColour;
                    lerpPosition = particle->Age / MiddlePosition;
                }
                // Otherwise, interpolate between MiddleColour and FinalColour...
                else
                {
                    lerpColourA = MiddleColour;
                    lerpColourB = FinalColour;
                    lerpPosition = (particle->Age - MiddlePosition) / (1f - MiddlePosition);
                }

                // Interpolate between the necessary colours...
                particle->Colour.X = lerpColourA.X + (lerpColourB.X - lerpColourA.X) * lerpPosition;
                particle->Colour.Y = lerpColourA.Y + (lerpColourB.Y - lerpColourA.Y) * lerpPosition;
                particle->Colour.Z = lerpColourA.Z + (lerpColourB.Z - lerpColourA.Z) * lerpPosition;
            }

            // Iterate to the next particle...
            particle++;
        }
    }
}