using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarquakeXNA.AnimationHelpers
{
    public class AnimationTrigger
    {
        protected bool triggered;
        public bool Triggered
        {
            get { return triggered; }
        }

        protected bool useTrigger;

        public void Trigger()
        {
            triggered = true;
        }

        public AnimationTrigger(bool UseTrigger)
        {
            useTrigger = UseTrigger;
        }
    }

    public class IntegerSubtractor : AnimationTrigger
    {
        private int initialValue;
        private int finalValue;
        private int stepSize;
        
        private int modifier;
        public int Modifier
        {
            get { return modifier; }
        }

        public byte ByteModifier
        {
            get
            {
                if (modifier > 255)
                {
                    return 255;
                }
                else if (modifier < 0)
                {
                    return 0;
                }

                return (byte)modifier;
            }
        }

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

        public void Reset()
        {
            modifier = initialValue;
            triggered = !useTrigger;
        }


        public void Update()
        {
            if (triggered)
            {
                if (modifier > finalValue)
                {
                    modifier = modifier - stepSize;
                }

                if (modifier <= finalValue)
                {
                    modifier = finalValue;
                }
            }
        }
    }

    public class IntegerAdder : AnimationTrigger
    {
        private int initialValue;
        private int finalValue;
        private int stepSize;

        private int modifier;
        public int Modifier
        {
            get { return modifier; }
        }

        public byte ByteModifier
        {
            get 
            {
                if (modifier > 255)
                {
                    return 255;
                }
                else if (modifier < 0)
                {
                    return 0;
                }

                return (byte)modifier;
            }
        }

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

        public void Reset()
        {
            modifier = initialValue;
            triggered = !useTrigger;
        }


        public void Update()
        {
            if (triggered)
            {
                if (modifier < finalValue)
                {
                    modifier = modifier + stepSize;
                }

                if (modifier >= finalValue)
                {
                    modifier = finalValue;
                }
            }
        }
    }

    public class FloatSubtractor : AnimationTrigger
    {
        private float initialValue;
        private float finalValue;
        private float stepSize;

        private float modifier;
        public float Modifier
        {
            get { return modifier; }
        }

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

        public void Reset()
        {
            modifier = initialValue;
            triggered = !useTrigger;
        }


        public void Update()
        {
            if (triggered)
            {
                if (modifier > finalValue)
                {
                    modifier = modifier - stepSize;
                }

                if (modifier <= finalValue)
                {
                    modifier = finalValue;
                }
            }
        }
    }

    public class FloatAdder : AnimationTrigger
    {
        private float initialValue;
        private float finalValue;
        private float stepSize;

        private float modifier;
        public float Modifier
        {
            get { return modifier; }
        }

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

        public void Reset()
        {
            modifier = initialValue;
            triggered = !useTrigger;
        }


        public void Update()
        {
            if (triggered)
            {
                if (modifier < finalValue)
                {
                    modifier = modifier + stepSize;
                }

                if (modifier >= finalValue)
                {
                    modifier = finalValue;
                }
            }
        }
    }
}
