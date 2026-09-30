// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Animations;

public class FloatSubtractor : AnimationTrigger
{
    private readonly float _finalValue;
    private readonly float _initialValue;
    private readonly float _stepSize;

    public FloatSubtractor(
        float initialValue,
        float finalValue,
        float stepSize,
        bool useTrigger) : base(useTrigger)
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
        Triggered = !UseTrigger;
    }


    public void Update()
    {
        if (Triggered)
        {
            if (Modifier > _finalValue)
            {
                Modifier = Modifier - _stepSize;
            }

            if (Modifier <= _finalValue)
            {
                Modifier = _finalValue;
            }
        }
    }
}
