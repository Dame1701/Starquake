using Microsoft.Xna.Framework.Audio;

namespace StarquakeXNA;

public class MultiInstanceSoundEffect
{
    public SoundEffect Sound;
    public int MaxInstances;
    public bool KillOnScreenChange;

    public MultiInstanceSoundEffect(SoundEffect sound, int maxInstances, bool killOnScreenChange)
    {
        Sound = sound;
        MaxInstances = maxInstances;
        KillOnScreenChange = killOnScreenChange;
    }
}