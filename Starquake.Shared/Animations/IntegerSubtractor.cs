// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;

namespace Starquake.Animations;

public class IntegerSubtractor : AnimationTrigger
{
    private readonly int _finalValue;
    private readonly int _initialValue;
    private readonly int _stepSize;

    public IntegerSubtractor(
        int initialValue,
        int finalValue,
        int stepSize,
        bool useTrigger) : base(useTrigger)
    {
        _initialValue = initialValue;
        _finalValue = finalValue;
        _stepSize = stepSize;

        Reset();
    }
    public int Modifier { get; private set; }

    public byte ByteModifier
    {
        get
        {
            if (Modifier > ColourLevels.Full)
            {
                return ColourLevels.Full;
            }
            if (Modifier < 0)
            {
                return 0;
            }

            return (byte)Modifier;
        }
    }

    public void Reset()
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