// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -

namespace StarquakeXNA.Helpers;

public class AnimationTrigger
{
    protected bool UseTrigger;

    public AnimationTrigger(bool useTrigger)
    {
        UseTrigger = useTrigger;
    }
    public bool Triggered { get; protected set; }

    public void Trigger()
    {
        Triggered = true;
    }
}

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
            if (Modifier > 255)
            {
                return 255;
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

public class IntegerAdder : AnimationTrigger
{
    private readonly int _finalValue;
    private readonly int _initialValue;
    private readonly int _stepSize;

    public IntegerAdder(
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
            if (Modifier > 255)
            {
                return 255;
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
            if (Modifier < _finalValue)
            {
                Modifier = Modifier + _stepSize;
            }

            if (Modifier >= _finalValue)
            {
                Modifier = _finalValue;
            }
        }
    }
}

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

public class FloatAdder : AnimationTrigger
{
    private readonly float _finalValue;
    private readonly float _initialValue;
    private readonly float _stepSize;

    public FloatAdder(
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

    public void Reset()
    {
        Modifier = _initialValue;
        Triggered = !UseTrigger;
    }


    public void Update()
    {
        if (Triggered)
        {
            if (Modifier < _finalValue)
            {
                Modifier = Modifier + _stepSize;
            }

            if (Modifier >= _finalValue)
            {
                Modifier = _finalValue;
            }
        }
    }
}