// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

/*
 Copyright � 2009 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

namespace ProjectMercury.Modifiers;

/// <summary>
///     Defines a Modifier which alters the rotation of a Particle over its lifetime.
/// </summary>
#if WINDOWS
    [TypeConverter("ProjectMercury.Design.Modifiers.RotationModifierTypeConverter, ProjectMercury.Design")]
#endif
public sealed class RotationModifier : Modifier
{
    /// <summary>
    ///     The rate of rotation in radians per second.
    /// </summary>
    public float RotationRate;

    /// <summary>
    ///     Returns a deep copy of the Modifier implementation.
    /// </summary>
    /// <returns></returns>
    public override Modifier DeepCopy()
        => new RotationModifier
        {
            RotationRate = RotationRate
        };

    /// <summary>
    ///     Processes the particles.
    /// </summary>
    /// <param name="dt">Elapsed time in whole and fractional seconds.</param>
    /// <param name="particleArray">A pointer to an array of particles.</param>
    /// <param name="count">The number of particles which need to be processed.</param>
    internal protected override unsafe void Process(float dt, Particle* particleArray, int count)
    {
        float deltaRotation = RotationRate * dt;

        for (int i = 0; i < count; i++)
        {
            Particle* particle = particleArray + i;

            particle->Rotate(deltaRotation);
        }
    }
}