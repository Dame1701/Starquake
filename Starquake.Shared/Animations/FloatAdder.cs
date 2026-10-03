// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Animations;

internal class FloatAdder
{
    private readonly float _finalValue;
    private readonly float _initialValue;
    private readonly float _stepSize;

    public FloatAdder(
        float initialValue,
        float finalValue,
        float stepSize)
    {
        _initialValue = initialValue;
        _finalValue = finalValue;
        _stepSize = stepSize;

        Reset();
    }
    public float Modifier { get; private set; }

    private void Reset()
    {
        Modifier = _initialValue;
    }


    public void Update()
        => Modifier = Ramp.Up(Modifier, _stepSize, _finalValue);
}
