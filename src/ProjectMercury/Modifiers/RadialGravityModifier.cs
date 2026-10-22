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
///     Defines a Modifier which pulls Particles towards it.
/// </summary>
#if WINDOWS
    [TypeConverter("ProjectMercury.Design.Modifiers.RadialGravityModifierTypeConverter, ProjectMercury.Design")]
#endif
public sealed class RadialGravityModifier : Modifier
{
    /// <summary>
    ///     The position of the gravity well.
    /// </summary>
    public Vector2 Position;

    private float _radius;

    /// <summary>
    ///     Gets or sets the radius of the gravity well.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown if the specified value is negetive or zero.</exception>
    public float Radius
    {
        get => _radius;
        set
        {
            Guard.ArgumentNotFinite("Radius", value);
            Guard.ArgumentLessThan("Radius", value, float.Epsilon);

            _radius = value;

            SquareRadius = value * value;
        }
    }

    private float SquareRadius;

    private float _innerRadius;

    /// <summary>
    ///     Gets or sets the inner radius of the gravity well, within which Particles will not be attracted.
    /// </summary>
    public float InnerRadius
    {
        get => _innerRadius;
        set
        {
            Guard.ArgumentNotFinite("InnerRadius", value);
            Guard.ArgumentLessThan("InnerRadius", value, 0f);
            Guard.ArgumentGreaterThan("InnerRadius", value, Radius);

            _innerRadius = value;

            SquareInnerRadius = value * value;
        }
    }

    private float SquareInnerRadius;

    /// <summary>
    ///     The strength of the gravity well.
    /// </summary>
    public float Strength;

    /// <summary>
    ///     Returns a deep copy of the Modifier implementation.
    /// </summary>
    /// <returns></returns>
    public override Modifier DeepCopy()
        => new RadialGravityModifier
        {
            Radius = Radius,
            InnerRadius = InnerRadius,
            Position = Position,
            Strength = Strength
        };

    /// <summary>
    ///     Processes the particles.
    /// </summary>
    /// <param name="dt">Elapsed time in whole and fractional seconds.</param>
    /// <param name="particleArray">A pointer to an array of particles.</param>
    /// <param name="count">The number of particles which need to be processed.</param>
    internal protected override unsafe void Process(float dt, Particle* particleArray, int count)
    {
        float strengthDelta = Strength * dt;

        for (int i = 0; i < count; i++)
        {
            Particle* particle = particleArray + i;

            // Calculate the distance between the Particle and the center of the gravity well...
            var distance = new Vector2
            {
                X = Position.X - particle->Position.X,
                Y = Position.Y - particle->Position.Y
            };

            float squareDistance = distance.X * distance.X + distance.Y * distance.Y;

            // Check to see if the Particle is within range...
            if (squareDistance < SquareRadius && squareDistance > SquareInnerRadius)
            {
                // Generates a normalised force vector from the Particle towards the centre of the gravity well...
                float length = Calculator.Sqrt(squareDistance);

                var force = new Vector2
                {
                    X = distance.X / length,
                    Y = distance.Y / length
                };

                // Diminish the force vector based on the distance from the centre of the gravity well...
                float relativeDistance = SquareRadius / squareDistance;

                force.X *= relativeDistance;
                force.Y *= relativeDistance;

                // Multiply the force vector based on the strength of the gravity well...
                force.X *= strengthDelta;
                force.Y *= strengthDelta;

                // Apply the force to the Particle...
                particle->Velocity.X += force.X;
                particle->Velocity.Y += force.Y;
            }
        }
    }
}