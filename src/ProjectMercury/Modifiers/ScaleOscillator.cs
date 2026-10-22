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
///     Defines a Modifier which adjusts the scale of Particles based on a sine wave.
/// </summary>
#if WINDOWS
    [TypeConverter("ProjectMercury.Design.Modifiers.ScaleOscillatorTypeConverter, ProjectMercury.Design")]
#endif
public class ScaleOscillator : Modifier
{
    private float TotalSeconds;

    private float _frequency;

    /// <summary>
    ///     Gets or sets the oscillator frequency (the number of cycles per second).
    /// </summary>
    public float Frequency
    {
        get => _frequency;
        set
        {
            Guard.ArgumentNotFinite("Frequency", value);
            Guard.ArgumentLessThan("Frequency", value, float.Epsilon);

            _frequency = value;
        }
    }

    private float _minimum;

    /// <summary>
    ///     Gets or sets the minimum scale (the scale of Particles at the negetive peak of the sine wave).
    /// </summary>
    /// <value>The minimum scale.</value>
    public float MinimumScale
    {
        get => _minimum;
        set
        {
            Guard.ArgumentNotFinite("MinimumScale", value);
            Guard.ArgumentLessThan("MinimumScale", value, 0f);

            _minimum = value;
        }
    }

    private float _maximum;

    /// <summary>
    ///     Gets or sets the maximum scale (the scale of Particles at the positive peak of the sine wave).
    /// </summary>
    /// <value>The maximum scale.</value>
    public float MaximumScale
    {
        get => _maximum;
        set
        {
            Guard.ArgumentNotFinite("MaximumScale", value);
            Guard.ArgumentLessThan("MaximumScale", value, 0f);

            _maximum = value;
        }
    }

    /// <summary>
    ///     Returns a deep copy of the Modifier implementation.
    /// </summary>
    /// <returns></returns>
    public override Modifier DeepCopy()
        => new ScaleOscillator
        {
            Frequency = Frequency,
            MinimumScale = MinimumScale,
            MaximumScale = MaximumScale
        };

    /// <summary>
    ///     Processes the particles.
    /// </summary>
    /// <param name="dt">Elapsed time in whole and fractional seconds.</param>
    /// <param name="particleArray">A pointer to an array of particles.</param>
    /// <param name="count">The number of particles which need to be processed.</param>
    internal protected override unsafe void Process(float dt, Particle* particleArray, int count)
    {
        TotalSeconds += dt;

        for (int i = 0; i < count; i++)
        {
            Particle* particle = particleArray + i;

            float secondsAlive = TotalSeconds - particle->Inception;

            float sin = Calculator.Sin(secondsAlive * (Frequency * 3f));

            particle->Scale = (MaximumScale - MinimumScale) * sin + MinimumScale;
        }
    }
}