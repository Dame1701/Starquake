// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework.Audio;

namespace StarquakeXNA;

public class SoundEffectInstanceRecord
{
    public readonly bool KillOnScreenChange;
    public readonly SoundEffectInstance SoundInstance;
    public readonly string SoundName;
    public Guid Id;
    public enSoundEffectInstanceType InstanceType;
    public long PlayStart;
    public float Volume;

    public SoundEffectInstanceRecord(string soundName, enSoundEffectInstanceType instanceType,
        SoundEffectInstance soundInstance, long playStart, float volume, bool killOnScreenChange)
    {
        SoundName = soundName;
        InstanceType = instanceType;
        SoundInstance = soundInstance;
        PlayStart = playStart;
        Volume = volume;
        KillOnScreenChange = killOnScreenChange;
    }
}