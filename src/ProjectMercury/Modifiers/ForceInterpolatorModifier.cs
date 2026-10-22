// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

/*
 Copyright © 2010 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)

 This program is licensed under the Microsoft Permissive License (Ms-PL).  You should
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://mpe.codeplex.com/license.
*/

using Microsoft.Xna.Framework;

namespace ProjectMercury.Modifiers;

/// <summary>
///     Defines a modifier which changes the Force of particles based on a linear interpolation over three values.
/// </summary>
#if WINDOWS
    [TypeConverter("ProjectMercury.Design.Modifiers.ForceInterpolatorModifierTypeConverter, ProjectMercury.Design")]
#endif
public class ForceInterpolatorModifier : Modifier
{

    /// <summary>
    ///     Gets or sets the initial force vector.
    /// </summary>
    /// <value>The initial force vector.</value>
    public Vector2 InitialForce { get; set; }

    /// <summary>
    ///     Gets or sets the middle force vector.
    /// </summary>
    /// <value>The middle force vector.</value>
    public Vector2 MiddleForce { get; set; }

    private float _middlePosition;

    /// <summary>
    ///     Gets or sets the middle force position.
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
    ///     Gets or sets the final force vector.
    /// </summary>
    /// <value>The final force vector.</value>
    public Vector2 FinalForce { get; set; }

    /// <summary>
    ///     Returns a deep copy of the Modifier implementation.
    /// </summary>
    /// <returns></returns>
    public override Modifier DeepCopy()
        => new ForceInterpolatorModifier
        {
            InitialForce = InitialForce,
            MiddleForce = MiddleForce,
            MiddlePosition = MiddlePosition,
            FinalForce = FinalForce
        };

    /// <summary>
    ///     Processes the particles.
    /// </summary>
    /// <param name="dt">Elapsed time in whole and fractional seconds.</param>
    /// <param name="particleArray">A pointer to an array of particles.</param>
    /// <param name="count">The number of particles which need to be processed.</param>
    internal protected override unsafe void Process(float dt, Particle* particleArray, int count)
    {

        // Daniel, I think it would be better here to use the elapsed time to get a delta of the initial, middle and final forces,
        // and use those values for the interpolation below...
        // Vector2 initialForceDelta = this.InitialForce * dt;
        // Vector2 middleForceDelta = this.MiddleForce * dt;
        // Vector2 finalForceDelta = this.FinalForce * dt;
        // This would make it more consistent with the other force modifiers which express forces as value/seconds.
        // @Matt: i borrowed the calculation from one of the other modifiers but that doesn't mean i was right. i will change that

        for (int i = 0; i < count; i++)
        {
            Particle* particle = particleArray + i;

            // Also, the previous particle optimisation doesn't work in this case, as there could be multiple forces
            // acting on particles in different places. So even if particles were released at the same time, they could
            // have difference velocities...
            // @Matt: you are right. will change this with the next update. in the mean time i optimized the calculation of the 
            // positionDelta. it's calculated only once now
            Particle* previousParticle = particle - 1;

            if (particle->Age == previousParticle->Age)
            {
                particle->Velocity.X = previousParticle->Velocity.X;
                particle->Velocity.Y = previousParticle->Velocity.Y;

                continue;
            }

            if (particle->Age < MiddlePosition)
            {
                float positionDelta = particle->Age / MiddlePosition;
                particle->Velocity.X += InitialForce.X + (MiddleForce.X - InitialForce.X) * positionDelta;
                particle->Velocity.Y += InitialForce.Y + (MiddleForce.Y - InitialForce.Y) * positionDelta;

            }
            else
            {
                float positionDelta = (particle->Age - MiddlePosition) / (1f - MiddlePosition);
                particle->Velocity.X += MiddleForce.X + (FinalForce.X - MiddleForce.X) * positionDelta;
                particle->Velocity.Y += MiddleForce.Y + (FinalForce.Y - MiddleForce.Y) * positionDelta;
            }
        }
    }
}