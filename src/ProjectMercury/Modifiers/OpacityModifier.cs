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
///     Defines a Modifier which gradually changes the opacity of a Particle over its lifetime.
/// </summary>
#if WINDOWS
    [TypeConverter("ProjectMercury.Design.Modifiers.OpacityModifierTypeConverter, ProjectMercury.Design")]
#endif
public sealed class OpacityModifier : Modifier
{
    private float _initial;

    /// <summary>
    ///     Gets or sets the initial opacity of Particles as they are released.
    /// </summary>
    public float Initial
    {
        get => _initial;
        set
        {
            Guard.ArgumentNotFinite("Initial", value);
            Guard.ArgumentLessThan("Initial", value, 0f);
            Guard.ArgumentGreaterThan("Initial", value, 1f);

            _initial = value;
        }
    }

    private float _ultimate;

    /// <summary>
    ///     Gets or sets the ultimate opacity of Particles as they are retired.
    /// </summary>
    public float Ultimate
    {
        get => _ultimate;
        set
        {
            Guard.ArgumentNotFinite("Ultimate", value);
            Guard.ArgumentLessThan("Ultimate", value, 0f);
            Guard.ArgumentGreaterThan("Ultimate", value, 1f);

            _ultimate = value;
        }
    }

    /// <summary>
    ///     Returns a deep copy of the Modifier implementation.
    /// </summary>
    /// <returns></returns>
    public override Modifier DeepCopy()
        => new OpacityModifier
        {
            Initial = Initial,
            Ultimate = Ultimate
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

            particle->Colour.W = Initial + (Ultimate - Initial) * particle->Age;
        }
    }
}