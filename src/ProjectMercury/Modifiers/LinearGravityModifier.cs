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
///     Defines a Modifier that applies a constant force vector to Particles over their lifetime.
/// </summary>
#if WINDOWS
    [TypeConverter("ProjectMercury.Design.Modifiers.LinearGravityModifierTypeConverter, ProjectMercury.Design")]
#endif
public sealed class LinearGravityModifier : Modifier
{
    /// <summary>
    ///     Returns a deep copy of the Modifier implementation.
    /// </summary>
    /// <returns></returns>
    public override Modifier DeepCopy()
        => new LinearGravityModifier
        {
            Gravity = Gravity
        };

    /// <summary>
    ///     Gets or sets the gravity vector.
    /// </summary>
    public Vector2 Gravity;

    /// <summary>
    ///     Processes the particles.
    /// </summary>
    /// <param name="dt">Elapsed time in whole and fractional seconds.</param>
    /// <param name="particleArray">A pointer to an array of particles.</param>
    /// <param name="count">The number of particles which need to be processed.</param>
    internal protected override unsafe void Process(float dt, Particle* particleArray, int count)
    {
        float deltaGravityX = Gravity.X * dt;
        float deltaGravityY = Gravity.Y * dt;

        for (int i = 0; i < count; i++)
        {
            Particle* particle = particleArray + i;

            particle->Velocity.X += deltaGravityX;
            particle->Velocity.Y += deltaGravityY;
        }
    }
}