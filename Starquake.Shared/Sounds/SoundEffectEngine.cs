// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework.Audio;
using Starquake.ContentManagement;
using Starquake.Engines;
using Starquake.Structure;

namespace Starquake.Sounds;

public class SoundEffectEngine(GameEngine gameEngineInstance) : IDisposable
{

    private readonly List<SoundEffectInstanceRecord> _multiInstanceSoundEffectInstances = [];
    private readonly Dictionary<string, MultiInstanceSoundEffect> _multiInstanceSoundEffects = new();
    private readonly List<SoundEffectInstanceRecord> _singleInstanceSoundEffectInstances = [];
    private bool _periodicBackgroundSoundsEnabled;
    private Guid _periodicBackgroundSoundsId = Guid.Empty;
    private Timer _periodicSoundTimer;

    public void Dispose()
    {
        if (_periodicSoundTimer == null)
        {
            return;
        }

        _periodicSoundTimer.Dispose();
        _periodicSoundTimer = null;
    }

    public void LoadSoundEffect(SoundDescription soundDesc)
    {
        switch (soundDesc.InstanceType)
        {
            case SoundEffectInstanceType.Single:
            {
                var sfx = gameEngineInstance.ContentMgr.Load<SoundEffect>(soundDesc.FilePath);
                var soundEffectInstanceRecord = new SoundEffectInstanceRecord(soundDesc.ResourceName,
                    soundDesc.InstanceType, sfx.CreateInstance(), 0, 0, soundDesc.KillOnScreenChange)
                {
                    Id = Guid.NewGuid()
                };

                _singleInstanceSoundEffectInstances.Add(soundEffectInstanceRecord);
                break;
            }
            case SoundEffectInstanceType.Multi:
                _multiInstanceSoundEffects.Add(soundDesc.ResourceName,
                    new MultiInstanceSoundEffect(gameEngineInstance.ContentMgr.Load<SoundEffect>(soundDesc.FilePath), 4,
                        soundDesc.KillOnScreenChange));
                break;
        }
    }

    public void EnablePeriodicBackgroundSounds()
    {
        if (_periodicBackgroundSoundsEnabled)
        {
            return;
        }

        _periodicBackgroundSoundsEnabled = true;
        _periodicSoundTimer = new Timer(PlayPeriodicSound, null,
            gameEngineInstance.RandomNumberGenerator.Next(1000, 10000), Timeout.Infinite);
    }

    public void DisablePeriodicBackgroundSounds(bool killPlayingSounds = false)
    {
        if (!_periodicBackgroundSoundsEnabled)
        {
            return;
        }

        if (_periodicSoundTimer != null)
        {
            _periodicSoundTimer.Dispose();
            _periodicSoundTimer = null;
        }

        if (killPlayingSounds)
        {
            StopSound(_periodicBackgroundSoundsId, SoundFade.None);
        }

        _periodicBackgroundSoundsEnabled = false;
    }

    private void PlayPeriodicSound(object state)
    {
        if (GetSoundState(_periodicBackgroundSoundsId) == SoundState.Playing)
        {
            return;
        }

        _periodicBackgroundSoundsId = PlaySound("BackgroundSound1", 0.5f, false);
        _periodicSoundTimer = new Timer(PlayPeriodicSound, null,
            gameEngineInstance.RandomNumberGenerator.Next(10000, 20000), Timeout.Infinite);
    }

    /// <summary>
    ///     Kills all sounds currently playing
    /// </summary>
    public void KillSounds(bool paused)
    {
        if (!paused)
        {
            // kill single instance sounds
            foreach (var soundEffectInstanceRecord in _singleInstanceSoundEffectInstances)
            {
                if (!soundEffectInstanceRecord.KillOnScreenChange)
                {
                    continue;
                }

                if (soundEffectInstanceRecord.SoundInstance.State != SoundState.Stopped)
                {
                    soundEffectInstanceRecord.SoundInstance.Stop();
                }
            }

            // kill multi instance sounds
            foreach (var soundEffectInstanceRecord in _multiInstanceSoundEffectInstances)
            {
                if (!soundEffectInstanceRecord.KillOnScreenChange)
                {
                    continue;
                }

                if (soundEffectInstanceRecord.SoundInstance.State != SoundState.Stopped)
                {
                    soundEffectInstanceRecord.SoundInstance.Stop();
                }
            }
        }
        else
        {
            // kill single instance sounds
            foreach (var soundEffectInstanceRecord in _singleInstanceSoundEffectInstances)
            {
                if (soundEffectInstanceRecord.SoundInstance.State != SoundState.Stopped)
                {
                    soundEffectInstanceRecord.SoundInstance.Stop();
                }
            }

            // kill multi instance sounds
            foreach (var soundEffectInstanceRecord in _multiInstanceSoundEffectInstances)
            {
                if (soundEffectInstanceRecord.SoundInstance.State != SoundState.Stopped)
                {
                    soundEffectInstanceRecord.SoundInstance.Stop();
                }
            }
        }
    }

    public void StopSound(Guid id, SoundFade soundFade)
    {
        var soundEffectInstanceRecord = _singleInstanceSoundEffectInstances.FirstOrDefault(sfx => sfx.Id == id);
        if (soundEffectInstanceRecord != null)
        {
            if (soundEffectInstanceRecord.SoundInstance.State != SoundState.Playing)
            {
                return;
            }

            if (soundEffectInstanceRecord.Volume < 0)
            {
                soundEffectInstanceRecord.SoundInstance.Stop();
            }
            else
            {
                switch (soundFade)
                {
                    case SoundFade.None:
                        soundEffectInstanceRecord.Volume = 0;
                        break;
                    case SoundFade.Medium:
                        soundEffectInstanceRecord.Volume -= 0.05F;
                        break;
                    case SoundFade.Fast:
                        soundEffectInstanceRecord.Volume -= 0.1F;
                        break;
                    case SoundFade.Slow:
                        soundEffectInstanceRecord.Volume -= 0.025F;
                        break;
                }

                if (soundEffectInstanceRecord.Volume > 0)
                {
                    soundEffectInstanceRecord.SoundInstance.Volume = soundEffectInstanceRecord.Volume;
                }
                else
                {
                    soundEffectInstanceRecord.SoundInstance.Volume = 0;
                }
            }

            return;
        }

        foreach (var t in _multiInstanceSoundEffectInstances)
        {
            if (t.Id != id)
            {
                continue;
            }

            if (t.SoundInstance.State == SoundState.Stopped)
            {
                continue;
            }

            t.SoundInstance.Volume = 0f;
            t.SoundInstance.Stop();
            break;
        }
    }

    public Guid PlaySound(string soundName, bool looped)
        => PlaySound(soundName, 1F, looped);

    public Guid PlaySound(string soundName, float volume)
        => PlaySound(soundName, volume, false);

    public Guid PlaySound(string soundName, float volume, bool looped)
    {
        Guid singleInstanceId;
        SoundEffectInstanceRecord soundEffectInstanceRecord = null;

        foreach (var t in _singleInstanceSoundEffectInstances)
        {
            if (t.SoundName != soundName)
            {
                continue;
            }

            soundEffectInstanceRecord = t;
            soundEffectInstanceRecord.Volume = volume;
            break;
        }

        if (soundEffectInstanceRecord != null)
        {
            if (soundEffectInstanceRecord.SoundInstance == null)
            {
                return Guid.Empty;
            }

            singleInstanceId = soundEffectInstanceRecord.Id;

            // If the sound is currently playing or paused, reset it
            if (!looped)
            {
                if (soundEffectInstanceRecord.SoundInstance.State == SoundState.Playing ||
                    soundEffectInstanceRecord.SoundInstance.State == SoundState.Paused)
                {
                    soundEffectInstanceRecord.SoundInstance.Stop(true);

                    // Hack to make the sounds actually stop!
                    //do { } while (soundEffectInstanceRecord.SoundInstance.State != SoundState.Stopped);
                }
            }

            // Play the sound
            soundEffectInstanceRecord.SoundInstance.Volume = volume;

            if (!looped)
            {
                soundEffectInstanceRecord.SoundInstance.Play();
            }
            else if (soundEffectInstanceRecord.SoundInstance.State == SoundState.Stopped ||
                     soundEffectInstanceRecord.SoundInstance.State == SoundState.Paused)
            {
                soundEffectInstanceRecord.SoundInstance.Play();
            }
        }
        else
        {
            // Multi instance sound effects
            if (!_multiInstanceSoundEffects.TryGetValue(soundName, out var misxf))
            {
                return Guid.Empty;
            }

            // Create a new instance, but maybe keep a list of all instances created so we can put a max seal of the number of instances per sound allowed
            if (misxf == null)
            {
                return Guid.Empty;
            }

            // Check number of active slots
            SoundEffectInstanceRecord soundToPlay;
            var temp = new List<SoundEffectInstanceRecord>();

            foreach (var t in _multiInstanceSoundEffectInstances)
            {
                if (t.SoundName == soundName)
                {
                    temp.Add(t);
                }
            }

            if (temp.Count > 1 && temp.Count >= misxf.MaxInstances)
            {
                // Kill off the one playing the longest
                soundToPlay = temp[0];
                for (var a = 1; a < temp.Count; a++)
                {
                    if (temp[a].PlayStart < soundToPlay.PlayStart)
                    {
                        soundToPlay = temp[a];
                    }
                }

                // Stop the sound playing
                soundToPlay?.SoundInstance.Stop();

                // Update the playtime - this should update the master list automatically since we have kept the same reference
                if (soundToPlay != null)
                {
                    soundToPlay.PlayStart = DateTime.Now.Ticks;
                }
            }
            else
            {
                // Only create a new one if we have remaining slots
                soundToPlay = new SoundEffectInstanceRecord(soundName, SoundEffectInstanceType.Multi,
                    misxf.Sound.CreateInstance(), DateTime.Now.Ticks, 0, misxf.KillOnScreenChange)
                {
                    // Set the ID of the sound we're playing
                    Id = Guid.NewGuid()
                };

                // Record the instance
                _multiInstanceSoundEffectInstances.Add(soundToPlay);
            }

            // Play the sound
            if (soundToPlay == null)
            {
                return Guid.Empty;
            }

            soundToPlay.SoundInstance.Volume = volume;
            soundToPlay.SoundInstance.Play();

            // Return the ID of the multisound
            return soundToPlay.Id;
        }

        return singleInstanceId;
    }

    public Guid PlaySound(string soundName)
        => PlaySound(soundName, 1, false);

    private SoundState GetSoundState(Guid id)
    {
        var soundEffectInstanceRecord = _singleInstanceSoundEffectInstances
            .FirstOrDefault(sfx => sfx.Id == id);

        return soundEffectInstanceRecord != null ? soundEffectInstanceRecord.SoundInstance.State : SoundState.Stopped;
    }
}