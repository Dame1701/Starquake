using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace StarquakeXNA
{
    public class SoundEffectInstanceRecord
    {
        public string SoundName;
        public SoundEffectInstance SoundInstance;
        public long PlayStart;
        public Guid ID;
        public bool KillOnScreenChange;
        public enSoundEffectInstanceType InstanceType;
        public float Volume;

        public SoundEffectInstanceRecord(string soundName, enSoundEffectInstanceType instanceType, SoundEffectInstance soundInstance, long playStart, float volume, bool killOnScreenChange)
        {
            SoundName = soundName;
            InstanceType = instanceType;
            SoundInstance = soundInstance;
            PlayStart = playStart;
            Volume = volume;
            KillOnScreenChange = killOnScreenChange;
        }
    }

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

    public class SoundEffectEngine : IDisposable
    {
        GameEngine gameEngineInstance;

        Dictionary<string, SoundEffectInstance> singleInstanceSoundEffects;
        Dictionary<string, MultiInstanceSoundEffect> multiInstanceSoundEffects;

        List<SoundEffectInstanceRecord> multiInstanceSoundEffectInstances;
        List<SoundEffectInstanceRecord> singleInstanceSoundEffectInstances;
        bool periodicBackgroundSoundsEnabled = false;
        Guid periodicBackgroundSoundsID = Guid.Empty;
        System.Threading.Timer periodicSoundTimer = null;

        public SoundEffectEngine(GameEngine GameEngineInstance)
        {
            gameEngineInstance = GameEngineInstance;
            singleInstanceSoundEffects = new Dictionary<string, SoundEffectInstance>();
            multiInstanceSoundEffects = new Dictionary<string, MultiInstanceSoundEffect>();

            multiInstanceSoundEffectInstances = new List<SoundEffectInstanceRecord>();
            singleInstanceSoundEffectInstances = new List<SoundEffectInstanceRecord>();
        }

        public void LoadSoundEffect(SoundDescription SoundDesc)
        {
            if (SoundDesc.InstanceType == enSoundEffectInstanceType.Single)
            {
                SoundEffect sfx = gameEngineInstance.ContentMgr.Load<SoundEffect>(SoundDesc.FilePath);
                SoundEffectInstanceRecord sfxir = new SoundEffectInstanceRecord(SoundDesc.ResourceName, SoundDesc.InstanceType, sfx.CreateInstance(), 0, 0, SoundDesc.KillOnScreenChange);
                sfxir.ID = Guid.NewGuid();

                singleInstanceSoundEffectInstances.Add(sfxir);
            }
            else if (SoundDesc.InstanceType == enSoundEffectInstanceType.Multi)
            {
                multiInstanceSoundEffects.Add(SoundDesc.ResourceName, new MultiInstanceSoundEffect(gameEngineInstance.ContentMgr.Load<SoundEffect>(SoundDesc.FilePath), 4, SoundDesc.KillOnScreenChange));
            }
        }

        public void EnablePeriodicBackgroundSounds()
        {
            if (periodicBackgroundSoundsEnabled == false)
            {
                periodicBackgroundSoundsEnabled = true;
                periodicSoundTimer = new System.Threading.Timer(PlayPeriodicSound, null, gameEngineInstance.RandomNumberGenerator.Next(1000, 10000), System.Threading.Timeout.Infinite);
            }
        }

        public void DisablePeriodicBackgroundSounds(bool KillPlayingSounds = false)
        {
            if (periodicBackgroundSoundsEnabled == true)
            {
                if (periodicSoundTimer !=null)
                {
                    periodicSoundTimer.Dispose();
                    periodicSoundTimer = null;
                }

                if (KillPlayingSounds)
                {
                    StopSound(periodicBackgroundSoundsID, enSoundFade.None);
                }

                periodicBackgroundSoundsEnabled = false;
            }
        }

        void PlayPeriodicSound(object state)
        {
            if (GetSoundState(periodicBackgroundSoundsID) != SoundState.Playing)
            {
                periodicBackgroundSoundsID = PlaySound("BackgroundSound1", 0.5f, false);
                periodicSoundTimer = new System.Threading.Timer(PlayPeriodicSound, null, gameEngineInstance.RandomNumberGenerator.Next(10000, 20000), System.Threading.Timeout.Infinite);
            }
        }

        /// <summary>
        /// Kills all sounds currently playing
        /// </summary>
        public void KillSounds(bool Paused)
        {
            if (!Paused)
            {
                // kill single instance sounds
                foreach (SoundEffectInstanceRecord sfxir in singleInstanceSoundEffectInstances)
                {
                    if (sfxir.KillOnScreenChange)
                    {
                        if (sfxir.SoundInstance.State != SoundState.Stopped)
                        {
                            sfxir.SoundInstance.Stop();
                        }
                    }
                }

                // kill multi instance sounds
                foreach (SoundEffectInstanceRecord sfxir in multiInstanceSoundEffectInstances)
                {
                    if (sfxir.KillOnScreenChange)
                    {
                        if (sfxir.SoundInstance.State != SoundState.Stopped)
                        {
                            sfxir.SoundInstance.Stop();
                        }
                    }
                }
            }
            else
            {
                // kill single instance sounds
                foreach (SoundEffectInstanceRecord sfxir in singleInstanceSoundEffectInstances)
                {

                    if (sfxir.SoundInstance.State != SoundState.Stopped)
                    {
                        sfxir.SoundInstance.Stop();
                    }
                }

                // kill multi instance sounds
                foreach (SoundEffectInstanceRecord sfxir in multiInstanceSoundEffectInstances)
                {

                    if (sfxir.SoundInstance.State != SoundState.Stopped)
                    {
                        sfxir.SoundInstance.Stop();
                    }
                }
            }
        }

        public void Update()
        {
            // Need to have an update method that stops the sounds for us automatically so the StopSound Method simply tags we need to stop one and update does the fade out etc. and eventually stops it
            // Same with Play, play just tags a sound to play while the update makes sure it's looped if looping is required
            // Can have stop method e.g. fade out or stop immediately maybe fad speeds as an enum too
        }

        // TODO
        public void StopSound(Guid ID, enSoundFade SoundFade)
        {
            SoundEffectInstanceRecord sfxir = singleInstanceSoundEffectInstances.Where(sfx => sfx.ID == ID).FirstOrDefault();

            if (sfxir !=null)
            {
                if (sfxir.SoundInstance.State == SoundState.Playing)
                {
                    if (sfxir.Volume < 0)
                    {
                        sfxir.SoundInstance.Stop();
                    }
                    else
                    {
                        if (SoundFade == enSoundFade.None)
                        {
                            sfxir.Volume = 0;
                        }
                        else if (SoundFade == enSoundFade.Medium)
                        {
                            sfxir.Volume = sfxir.Volume - 0.05F;
                        }
                        else if (SoundFade == enSoundFade.Fast)
                        {
                            sfxir.Volume = sfxir.Volume - 0.1F;
                        }
                        else if (SoundFade == enSoundFade.Slow)
                        {
                            sfxir.Volume = sfxir.Volume - 0.025F;
                        }

                        if (sfxir.Volume > 0)
                        {
                            sfxir.SoundInstance.Volume = sfxir.Volume;
                        }
                        else
                        {
                            sfxir.SoundInstance.Volume = 0;
                        }
                    }
                }

                return;
            }

            for (int a = 0; a < multiInstanceSoundEffectInstances.Count; a++)
            {
                if (multiInstanceSoundEffectInstances[a].ID == ID)
                {
                    if (multiInstanceSoundEffectInstances[a].SoundInstance.State != SoundState.Stopped)
                    {
                        multiInstanceSoundEffectInstances[a].SoundInstance.Volume = 0f;
                        multiInstanceSoundEffectInstances[a].SoundInstance.Stop();
                        break;
                    }
                }
            }
        }

        public Guid PlaySound(string SoundName, bool Looped)
        {
            return PlaySound(SoundName, 1F, Looped);
        }

        public Guid PlaySound(string SoundName, float Volume)
        {
            return PlaySound(SoundName, Volume, false);
        }

        public Guid PlaySound(string SoundName, float Volume, bool Looped)
        {
            Guid singleInstanceID = Guid.Empty;
            SoundEffectInstanceRecord sfxir = null;

            for (int a = 0; a < singleInstanceSoundEffectInstances.Count; a++)
            {
                if (singleInstanceSoundEffectInstances[a].SoundName == SoundName)
                {
                    sfxir = singleInstanceSoundEffectInstances[a];
                    sfxir.Volume = Volume;
                    break;
                }
            }

            if (sfxir !=null)
            {
                if (sfxir.SoundInstance != null)
                {
                    singleInstanceID = sfxir.ID;

                    // If the sound is currently playing or paused, reset it
                    if (!Looped)
                    {
                        if (sfxir.SoundInstance.State == SoundState.Playing ||
                            sfxir.SoundInstance.State == SoundState.Paused)
                        {                           
                            sfxir.SoundInstance.Stop(true);

                            // Hack to make the sounds actually stop!
                            //do { } while (sfxir.SoundInstance.State != SoundState.Stopped);
                        }
                    }

                    // Play the sound
                    sfxir.SoundInstance.Volume = Volume;

                    if (!Looped)
                    {
                        sfxir.SoundInstance.Play();
                    }
                    else if (sfxir.SoundInstance.State == SoundState.Stopped ||
                        sfxir.SoundInstance.State == SoundState.Paused)
                    {
                        sfxir.SoundInstance.Play();
                    }
                }
            }
            else
            {
                // Multi instance sound effects
                if (multiInstanceSoundEffects.ContainsKey(SoundName))
                {
                    // Create a new instance, but maybe keep a list of all instances created so we can put a max seal of the number of instances per sound allowed
                    MultiInstanceSoundEffect misxf = multiInstanceSoundEffects[SoundName];

                    if (misxf != null)
                    {
                        // Check number of active slots
                        SoundEffectInstanceRecord soundToPlay = null;

                        List<SoundEffectInstanceRecord> temp = new List<SoundEffectInstanceRecord>();

                        for (int a = 0; a < multiInstanceSoundEffectInstances.Count; a++)
                        {
                            if (multiInstanceSoundEffectInstances[a].SoundName == SoundName)
                            {
                                temp.Add(multiInstanceSoundEffectInstances[a]);
                            }
                        }

                        if (temp.Count > 1 && temp.Count >= misxf.MaxInstances)
                        {
                            // Kill off the one playing the longest
                            soundToPlay = temp[0];

                            for (int a = 1; a < temp.Count; a++)
                            {
                                if (temp[a].PlayStart < soundToPlay.PlayStart)
                                {
                                    soundToPlay = temp[a];
                                }
                            }

                            // Stop the sound playing
                            if (soundToPlay != null)
                            {
                                soundToPlay.SoundInstance.Stop();
                            }

                            // Update the playtime - this should update the master list automatically since we have kept the same reference
                            soundToPlay.PlayStart = DateTime.Now.Ticks;
                        }
                        else
                        {
                            // Only create a new one if we have remaining slots
                            soundToPlay = new SoundEffectInstanceRecord(SoundName, enSoundEffectInstanceType.Multi, misxf.Sound.CreateInstance(), DateTime.Now.Ticks, 0, misxf.KillOnScreenChange);

                            // Set the ID of the sound we're playing
                            soundToPlay.ID = Guid.NewGuid();

                            // Record the instance
                            multiInstanceSoundEffectInstances.Add(soundToPlay);
                        }

                        // Play the sound
                        soundToPlay.SoundInstance.Volume = Volume;
                        //soundToPlay.SoundInstance.IsLooped = Looped;
                        soundToPlay.SoundInstance.Play();

                        // Return the ID of the multisound
                        return soundToPlay.ID;
                    }
                }
            }

            return singleInstanceID;
        }

        public Guid PlaySound(string SoundName)
        {
            return PlaySound(SoundName, 1, false);
        }

        public SoundState GetSoundState(Guid ID)
        {
            SoundEffectInstanceRecord sfxir = singleInstanceSoundEffectInstances.Where(sfx => sfx.ID == ID).FirstOrDefault();

            if (sfxir !=null)
            {
                return sfxir.SoundInstance.State;
            }

            return SoundState.Stopped;
        }

        public void Dispose()
        {
            if (periodicSoundTimer !=null)
            {
                periodicSoundTimer.Dispose();
                periodicSoundTimer = null;
            }
        }
    }
}
