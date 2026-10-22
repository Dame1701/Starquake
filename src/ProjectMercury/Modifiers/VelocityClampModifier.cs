// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

/*
 Copyright © 2009 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

namespace ProjectMercury.Modifiers;

/// <summary>
///     Defines a Modifier which limits the velocity of Particles to a specified value.
/// </summary>
/// <remarks>
///     For best results insert this Modifier after any other Modifiers which may alter
///     the velocity of the Particle.
/// </remarks>
#if WINDOWS
    [TypeConverter("ProjectMercury.Design.Modifiers.VelocityClampModifierTypeConverter, ProjectMercury.Design")]
#endif
public class VelocityClampModifier : Modifier
{
    private float _maximumVelocity;

    private float SquareMaximumVelocity;

    /// <summary>
    ///     Gets or sets the maximum velocity of Particles..
    /// </summary>
    /// <value>The maximum velocity of Particles..</value>
    public float MaximumVelocity
    {
        get => _maximumVelocity;
        set
        {
            Guard.ArgumentNotFinite("MaximumVelocity", value);
            Guard.ArgumentLessThan("MaximumVelocity", value, 0f);

            _maximumVelocity = value;

            SquareMaximumVelocity = value * value;
        }
    }

    /// <summary>
    ///     Returns a deep copy of the Modifier implementation.
    /// </summary>
    /// <returns></returns>
    public override Modifier DeepCopy()
        => new VelocityClampModifier
        {
            MaximumVelocity = MaximumVelocity
        };

    /// <summary>
    ///     Processes the particles.
    /// </summary>
    /// <param name="dt">Elapsed time in whole and fractional seconds.</param>
    /// <param name="particleArray">A pointer to an array of particles.</param>
    /// <param name="count">The number of particles which need to be processed.</param>
    internal protected override unsafe void Process(float dt, Particle* particleArray, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Particle* particle = particleArray + i;

            float squareVelocity = particle->Velocity.X * particle->Velocity.X + particle->Velocity.Y * particle->Velocity.Y;

            if (squareVelocity > SquareMaximumVelocity)
            {
                float velocity = Calculator.Sqrt(squareVelocity);

                particle->Velocity.X = particle->Velocity.X / velocity * MaximumVelocity;
                particle->Velocity.Y = particle->Velocity.Y / velocity * MaximumVelocity;
            }
        }
    }
}