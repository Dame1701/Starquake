// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace StarquakeXNA.AnimationHelpers;

public class AnimationTrigger
{
    protected bool triggered;

    protected bool useTrigger;

    public AnimationTrigger(bool UseTrigger)
    {
        useTrigger = UseTrigger;
    }
    public bool Triggered => triggered;

    public void Trigger()
    {
        triggered = true;
    }
}

public class IntegerSubtractor : AnimationTrigger
{
    private readonly int finalValue;
    private readonly int initialValue;
    private readonly int stepSize;

    public IntegerSubtractor(
        int InitialValue,
        int FinalValue,
        int StepSize,
        bool UseTrigger) : base(UseTrigger)
    {
        initialValue = InitialValue;
        finalValue = FinalValue;
        stepSize = StepSize;

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
        Modifier = initialValue;
        triggered = !useTrigger;
    }


    public void Update()
    {
        if (triggered)
        {
            if (Modifier > finalValue)
            {
                Modifier = Modifier - stepSize;
            }

            if (Modifier <= finalValue)
            {
                Modifier = finalValue;
            }
        }
    }
}

public class IntegerAdder : AnimationTrigger
{
    private readonly int finalValue;
    private readonly int initialValue;
    private readonly int stepSize;

    public IntegerAdder(
        int InitialValue,
        int FinalValue,
        int StepSize,
        bool UseTrigger) : base(UseTrigger)
    {
        initialValue = InitialValue;
        finalValue = FinalValue;
        stepSize = StepSize;

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
        Modifier = initialValue;
        triggered = !useTrigger;
    }


    public void Update()
    {
        if (triggered)
        {
            if (Modifier < finalValue)
            {
                Modifier = Modifier + stepSize;
            }

            if (Modifier >= finalValue)
            {
                Modifier = finalValue;
            }
        }
    }
}

public class FloatSubtractor : AnimationTrigger
{
    private readonly float finalValue;
    private readonly float initialValue;
    private readonly float stepSize;

    public FloatSubtractor(
        float InitialValue,
        float FinalValue,
        float StepSize,
        bool UseTrigger) : base(UseTrigger)
    {
        initialValue = InitialValue;
        finalValue = FinalValue;
        stepSize = StepSize;

        Reset();
    }
    public float Modifier { get; private set; }

    public void Reset()
    {
        Modifier = initialValue;
        triggered = !useTrigger;
    }


    public void Update()
    {
        if (triggered)
        {
            if (Modifier > finalValue)
            {
                Modifier = Modifier - stepSize;
            }

            if (Modifier <= finalValue)
            {
                Modifier = finalValue;
            }
        }
    }
}

public class FloatAdder : AnimationTrigger
{
    private readonly float finalValue;
    private readonly float initialValue;
    private readonly float stepSize;

    public FloatAdder(
        float InitialValue,
        float FinalValue,
        float StepSize,
        bool UseTrigger) : base(UseTrigger)
    {
        initialValue = InitialValue;
        finalValue = FinalValue;
        stepSize = StepSize;

        Reset();
    }
    public float Modifier { get; private set; }

    public void Reset()
    {
        Modifier = initialValue;
        triggered = !useTrigger;
    }


    public void Update()
    {
        if (triggered)
        {
            if (Modifier < finalValue)
            {
                Modifier = Modifier + stepSize;
            }

            if (Modifier >= finalValue)
            {
                Modifier = finalValue;
            }
        }
    }
}