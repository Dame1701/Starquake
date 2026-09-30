// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Audio;

namespace Starquake.Sounds;

public class SoundEffectInstanceRecord
{
    public readonly bool KillOnScreenChange;
    public readonly SoundEffectInstance SoundInstance;
    public readonly string SoundName;
    public Guid Id;
    public long PlayStart;
    public float Volume;

    public SoundEffectInstanceRecord(string soundName, SoundEffectInstance soundInstance, long playStart,
        float volume, bool killOnScreenChange)
    {
        SoundName = soundName;
        SoundInstance = soundInstance;
        PlayStart = playStart;
        Volume = volume;
        KillOnScreenChange = killOnScreenChange;
    }
}
