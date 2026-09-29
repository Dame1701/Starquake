// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework.Audio;
using StarquakeXNA.Structure;

namespace StarquakeXNA.Sounds;

public class SoundEffectEngine(GameEngine GameEngineInstance) : IDisposable
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

    public void LoadSoundEffect(SoundDescription SoundDesc)
    {
        switch (SoundDesc.InstanceType)
        {
            case enSoundEffectInstanceType.Single:
            {
                var sfx = GameEngineInstance.ContentMgr.Load<SoundEffect>(SoundDesc.FilePath);
                var soundEffectInstanceRecord = new SoundEffectInstanceRecord(SoundDesc.ResourceName,
                    SoundDesc.InstanceType, sfx.CreateInstance(), 0, 0, SoundDesc.KillOnScreenChange)
                {
                    Id = Guid.NewGuid()
                };

                _singleInstanceSoundEffectInstances.Add(soundEffectInstanceRecord);
                break;
            }
            case enSoundEffectInstanceType.Multi:
                _multiInstanceSoundEffects.Add(SoundDesc.ResourceName,
                    new MultiInstanceSoundEffect(GameEngineInstance.ContentMgr.Load<SoundEffect>(SoundDesc.FilePath), 4,
                        SoundDesc.KillOnScreenChange));
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
            GameEngineInstance.RandomNumberGenerator.Next(1000, 10000), Timeout.Infinite);
    }

    public void DisablePeriodicBackgroundSounds(bool KillPlayingSounds = false)
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

        if (KillPlayingSounds)
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
            GameEngineInstance.RandomNumberGenerator.Next(10000, 20000), Timeout.Infinite);
    }

    /// <summary>
    ///     Kills all sounds currently playing
    /// </summary>
    public void KillSounds(bool Paused)
    {
        if (!Paused)
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

    public void StopSound(Guid Id, SoundFade SoundFade)
    {
        var soundEffectInstanceRecord = _singleInstanceSoundEffectInstances.FirstOrDefault(sfx => sfx.Id == Id);
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
                switch (SoundFade)
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
            if (t.Id != Id)
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

    public Guid PlaySound(string SoundName, bool Looped)
        => PlaySound(SoundName, 1F, Looped);

    public Guid PlaySound(string SoundName, float Volume)
        => PlaySound(SoundName, Volume, false);

    public Guid PlaySound(string SoundName, float Volume, bool Looped)
    {
        Guid singleInstanceId;
        SoundEffectInstanceRecord soundEffectInstanceRecord = null;

        foreach (var t in _singleInstanceSoundEffectInstances)
        {
            if (t.SoundName != SoundName)
            {
                continue;
            }

            soundEffectInstanceRecord = t;
            soundEffectInstanceRecord.Volume = Volume;
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
            if (!Looped)
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
            soundEffectInstanceRecord.SoundInstance.Volume = Volume;

            if (!Looped)
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
            if (!_multiInstanceSoundEffects.TryGetValue(SoundName, out var misxf))
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
                if (t.SoundName == SoundName)
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
                soundToPlay = new SoundEffectInstanceRecord(SoundName, enSoundEffectInstanceType.Multi,
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

            soundToPlay.SoundInstance.Volume = Volume;
            soundToPlay.SoundInstance.Play();

            // Return the ID of the multisound
            return soundToPlay.Id;
        }

        return singleInstanceId;
    }

    public Guid PlaySound(string SoundName)
        => PlaySound(SoundName, 1, false);

    private SoundState GetSoundState(Guid Id)
    {
        var soundEffectInstanceRecord = _singleInstanceSoundEffectInstances
            .FirstOrDefault(sfx => sfx.Id == Id);

        return soundEffectInstanceRecord != null ? soundEffectInstanceRecord.SoundInstance.State : SoundState.Stopped;
    }
}