// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Core;

// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.ActiveSprites;

/// <summary>
///     A value that travels from where it starts to where it is going, a step per logic frame, and
///     stays there. A core element shrinking out of the way, the one replacing it growing in, and
///     the fade that crosses between them are all the same thing with different numbers.
/// </summary>
/// <remarks>
///     The direction of travel follows from the two ends rather than being chosen by the caller.
///     Colour levels are whole numbers no larger than 255, so a fade steps through the same sequence
///     whether it is counted in floats or integers.
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
    ///     The value as a colour level, for ramps that are fades rather than scales. No clamp is
    ///     needed, because a ramp cannot leave the range it was given.
    /// </summary>
    public int ColourLevel => (int)Value;

    public void Update()
        => Value = _rampingUp
            ? Ramp.Up(Value, _stepSize, _finalValue)
            : Ramp.Down(Value, _stepSize, _finalValue);
}
