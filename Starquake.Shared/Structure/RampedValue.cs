// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Structure;

/// <summary>
///     A value that travels from where it starts to where it is going, a step per logic frame, and
///     stays there. A core element shrinking out of the way, the one replacing it growing in, and
///     the fade that crosses between them are all the same thing with different numbers.
/// </summary>
/// <remarks>
///     This was three classes - <c>FloatAdder</c>, <c>FloatSubtractor</c> and
///     <c>IntegerSubtractor</c> - identical but for a <c>+</c> against a <c>-</c> and a
///     <c>float</c> against an <c>int</c>. Which way it travels is not something a caller should
///     have to pick a class for: it follows from the two ends, and all three were constructed with
///     both ends given.
///     <para>
///         The integer one counted colour levels, which are whole numbers no larger than 255 and so
///         exactly representable - the sequence a fade steps through is the same either way.
///     </para>
/// </remarks>
internal class RampedValue
{
    private readonly float _finalValue;
    private readonly bool _rampingUp;
    private readonly float _stepSize;

    /// <param name="initialValue">Where the value starts, and what it holds before the first step.</param>
    /// <param name="finalValue">Where it is going, and where it stops.</param>
    /// <param name="stepSize">How far it moves each frame, as a magnitude.</param>
    public RampedValue(float initialValue, float finalValue, float stepSize)
    {
        _finalValue = finalValue;
        _stepSize = stepSize;
        _rampingUp = initialValue < finalValue;

        Value = initialValue;
    }

    public float Value { get; private set; }

    /// <summary>
    ///     The value as a colour level, for the ramps that are fades rather than scales. The ramp
    ///     cannot leave the range it was given, so this needs no clamp of its own - the one
    ///     <c>IntegerSubtractor.ByteModifier</c> carried could never fire.
    /// </summary>
    public int ColourLevel => (int)Value;

    public void Update()
        => Value = _rampingUp
            ? Ramp.Up(Value, _stepSize, _finalValue)
            : Ramp.Down(Value, _stepSize, _finalValue);
}
