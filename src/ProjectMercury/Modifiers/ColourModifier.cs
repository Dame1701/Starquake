// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

/*
 Copyright � 2009 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

using Microsoft.Xna.Framework;

namespace ProjectMercury.Modifiers;

/// <summary>
///     Defines a Modifier which gradually changes the colour of a Particle over the course of its lifetime.
/// </summary>
#if WINDOWS
    [TypeConverter("ProjectMercury.Design.Modifiers.ColourModifierTypeConverter, ProjectMercury.Design")]
#endif
public sealed class ColourModifier : Modifier
{
    /// <summary>
    ///     Returns a deep copy of the Modifier implementation.
    /// </summary>
    /// <returns></returns>
    public override Modifier DeepCopy()
        => new ColourModifier
        {
            InitialColour = InitialColour,
            UltimateColour = UltimateColour
        };

    /// <summary>
    ///     The initial colour of Particles when they are released.
    /// </summary>
    public Vector3 InitialColour;

    /// <summary>
    ///     The ultimate colour of Particles when they are retired.
    /// </summary>
    public Vector3 UltimateColour;

    /// <summary>
    ///     Processes the particles.
    /// </summary>
    /// <param name="elapsedSeconds">Elapsed time in whole and fractional seconds.</param>
    /// <param name="particle">A pointer to the first item in an array of particles.</param>
    /// <param name="count">The number of particles which need to be processed.</param>
    internal protected override unsafe void Process(float elapsedSeconds, Particle* particle, int count)
    {
        particle->Colour.X = InitialColour.X + (UltimateColour.X - InitialColour.X) * particle->Age;
        particle->Colour.Y = InitialColour.Y + (UltimateColour.Y - InitialColour.Y) * particle->Age;
        particle->Colour.Z = InitialColour.Z + (UltimateColour.Z - InitialColour.Z) * particle->Age;

        Particle* previousParticle = particle;

        particle++;

        for (int i = 1; i < count; i++)
        {
            if (particle->Age < previousParticle->Age)
            {
                particle->Colour.X = InitialColour.X + (UltimateColour.X - InitialColour.X) * particle->Age;
                particle->Colour.Y = InitialColour.Y + (UltimateColour.Y - InitialColour.Y) * particle->Age;
                particle->Colour.Z = InitialColour.Z + (UltimateColour.Z - InitialColour.Z) * particle->Age;
            }
            else
            {
                particle->Colour.X = previousParticle->Colour.X;
                particle->Colour.Y = previousParticle->Colour.Y;
                particle->Colour.Z = previousParticle->Colour.Z;
            }

            previousParticle++;
            particle++;
        }
    }
}