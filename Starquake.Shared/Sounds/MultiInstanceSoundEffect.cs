// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Audio;

namespace StarquakeXNA.Sounds;

public class MultiInstanceSoundEffect
{
    public bool KillOnScreenChange;
    public int MaxInstances;
    public SoundEffect Sound;

    public MultiInstanceSoundEffect(SoundEffect sound, int maxInstances, bool killOnScreenChange)
    {
        Sound = sound;
        MaxInstances = maxInstances;
        KillOnScreenChange = killOnScreenChange;
    }
}