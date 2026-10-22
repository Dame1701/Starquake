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
///     Defines a modifier which changes the opacity of particles based on a linear interpolation over three values.
/// </summary>
#if WINDOWS
    [TypeConverter("ProjectMercury.Design.Modifiers.OpacityInterpolatorModifierTypeConverter, ProjectMercury.Design")]
#endif
public class OpacityInterpolatorModifier : Modifier
{
    private float _initialOpacity;

    /// <summary>
    ///     Gets or sets the initial opacity.
    /// </summary>
    /// <value>The initial opacity.</value>
    public float InitialOpacity
    {
        get => _initialOpacity;
        set
        {
            Guard.ArgumentOutOfRange("InitialOpacity", value, 0f, 1f);

            _initialOpacity = value;
        }
    }

    private float _middleOpacity;

    /// <summary>
    ///     Gets or sets the middle opacity.
    /// </summary>
    /// <value>The middle opacity.</value>
    public float MiddleOpacity
    {
        get => _middleOpacity;
        set
        {
            Guard.ArgumentOutOfRange("MiddleOpacity", value, 0f, 1f);

            _middleOpacity = value;
        }
    }

    private float _middlePosition;

    /// <summary>
    ///     Gets or sets the middle opacity position.
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

    private float _finalOpacity;

    /// <summary>
    ///     Gets or sets the final opacity.
    /// </summary>
    /// <value>The final opacity.</value>
    public float FinalOpacity
    {
        get => _finalOpacity;
        set
        {
            Guard.ArgumentOutOfRange("FinalOpacity", value, 0f, 1f);

            _finalOpacity = value;
        }
    }

    /// <summary>
    ///     Returns a deep copy of the Modifier implementation.
    /// </summary>
    /// <returns></returns>
    public override Modifier DeepCopy()
        => new OpacityInterpolatorModifier
        {
            InitialOpacity = InitialOpacity,
            MiddleOpacity = MiddleOpacity,
            MiddlePosition = MiddlePosition,
            FinalOpacity = FinalOpacity
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

            Particle* previousParticle = particle - 1;

            if (particle->Age == previousParticle->Age)
            {
                particle->Colour.W = previousParticle->Colour.W;

                continue;
            }

            if (particle->Age < MiddlePosition)
            {
                particle->Colour.W = InitialOpacity + (MiddleOpacity - InitialOpacity) * (particle->Age / MiddlePosition);
            }

            else
            {
                particle->Colour.W = MiddleOpacity + (FinalOpacity - MiddleOpacity) * ((particle->Age - MiddlePosition) / (1f - MiddlePosition));
            }
        }
    }
}