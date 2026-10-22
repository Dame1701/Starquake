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

namespace ProjectMercury.Emitters;

/// <summary>
///     Defines an Emitter which releases Particles in a circle or ring shape.
/// </summary>
#if WINDOWS
    [TypeConverter("ProjectMercury.Design.Emitters.CircleEmitterTypeConverter, ProjectMercury.Design")]
#endif
public class CircleEmitter : Emitter
{
    private float _radius;

    /// <summary>
    ///     Defines the radius of the circle.
    /// </summary>
    public float Radius
    {
        get => _radius;
        set
        {
            Guard.ArgumentNotFinite("Radius", value);
            Guard.ArgumentLessThan("Radius", value, float.Epsilon);

            _radius = value;
        }
    }

    /// <summary>
    ///     True if particles should be spawned only on the edge of the circle, else false.
    /// </summary>
    public bool Ring;

    /// <summary>
    ///     True if particles should radiate away from the center of the circle, else false.
    /// </summary>
    public bool Radiate;

    /// <summary>
    ///     Returns an unitialised deep copy of the Emitter.
    /// </summary>
    /// <returns>A deep copy of the Emitter.</returns>
    public override Emitter DeepCopy()
    {
        var copy = new CircleEmitter
        {
            Radius = Radius,
            Radiate = Radiate,
            Ring = Ring
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
        Vector2 angle = RandomHelper.NextUnitVector();

        float radiusMultiplier = Ring ? Radius : Radius * RandomHelper.NextFloat();

        offset = new Vector2
        {
            X = angle.X * radiusMultiplier,
            Y = angle.Y * radiusMultiplier
        };

        force = Radiate ? angle : RandomHelper.NextUnitVector();
    }
}