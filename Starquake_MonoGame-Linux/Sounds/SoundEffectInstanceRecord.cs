using System;
using Microsoft.Xna.Framework.Audio;

namespace StarquakeXNA;

public class SoundEffectInstanceRecord
{
    public readonly string SoundName;
    public readonly SoundEffectInstance SoundInstance;
    public long PlayStart;
    public Guid Id;
    public readonly bool KillOnScreenChange;
    public enSoundEffectInstanceType InstanceType;
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