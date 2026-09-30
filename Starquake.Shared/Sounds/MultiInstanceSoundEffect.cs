// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Audio;

namespace Starquake.Sounds;

public class MultiInstanceSoundEffect
{
    public readonly bool KillOnScreenChange;
    public readonly int MaxInstances;
    public readonly SoundEffect Sound;

    public MultiInstanceSoundEffect(SoundEffect sound, int maxInstances, bool killOnScreenChange)
    {
        Sound = sound;
        MaxInstances = maxInstances;
        KillOnScreenChange = killOnScreenChange;
    }
}