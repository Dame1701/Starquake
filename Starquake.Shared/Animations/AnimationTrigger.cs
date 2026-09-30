// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Animations;

public class AnimationTrigger
{
    protected readonly bool UseTrigger;

    protected AnimationTrigger(bool useTrigger)
    {
        UseTrigger = useTrigger;
    }
    public bool Triggered { get; protected set; }

    public void Trigger()
    {
        Triggered = true;
    }
}