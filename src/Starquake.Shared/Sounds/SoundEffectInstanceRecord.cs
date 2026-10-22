// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework.Audio;

namespace Starquake.Sounds;

internal class SoundEffectInstanceRecord(string soundName,
    SoundEffectInstance soundInstance,
    long playStart,
    float volume,
    bool killOnScreenChange)
{
    public readonly bool KillOnScreenChange = killOnScreenChange;
    public readonly SoundEffectInstance SoundInstance = soundInstance;
    public readonly string SoundName = soundName;
    public Guid Id;
    public long PlayStart = playStart;
    public float Volume = volume;
}
