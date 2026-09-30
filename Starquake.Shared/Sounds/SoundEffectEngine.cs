// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Starquake.ContentManagement;
using Starquake.Structure;

namespace Starquake.Sounds;

public class SoundEffectEngine(ContentManager content, Random randomNumberGenerator) : IDisposable
{

    private readonly List<SoundEffectInstanceRecord> _multiInstanceSoundEffectInstances = [];
    private readonly Dictionary<string, MultiInstanceSoundEffect> _multiInstanceSoundEffects = new();
    private readonly List<SoundEffectInstanceRecord> _singleInstanceSoundEffectInstances = [];
    private bool _periodicBackgroundSoundsEnabled;
    private Guid _periodicBackgroundSoundsId = Guid.Empty;
    private Timer? _periodicSoundTimer;

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
                var sfx = content.Load<SoundEffect>(soundDesc.FilePath);
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
                    new MultiInstanceSoundEffect(content.Load<SoundEffect>(soundDesc.FilePath), 4,
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
            randomNumberGenerator.Next(1000, 10000), Timeout.Infinite);
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

    private void PlayPeriodicSound(object? state)
    {
        if (GetSoundState(_periodicBackgroundSoundsId) == SoundState.Playing)
        {
            return;
        }

        _periodicBackgroundSoundsId = PlaySound("BackgroundSound1", 0.5f, false);
        _periodicSoundTimer = new Timer(PlayPeriodicSound, null,
            randomNumberGenerator.Next(10000, 20000), Timeout.Infinite);
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
        var singleInstance = FindSingleInstance(soundName);

        if (singleInstance != null)
        {
            singleInstance.Volume = volume;

            return PlaySingleInstance(singleInstance, volume, looped);
        }

        return PlayMultiInstance(soundName, volume);
    }

    /// <summary>
    ///     The one instance reserved for a sound that may only play once at a time, or null if this
    ///     sound is not one of those.
    /// </summary>
    private SoundEffectInstanceRecord? FindSingleInstance(string soundName)
    {
        foreach (var t in _singleInstanceSoundEffectInstances)
        {
            if (t.SoundName == soundName)
            {
                return t;
            }
        }

        return null;
    }

    /// <summary>
    ///     Plays the sound on its own reserved instance. A one-shot is restarted from the beginning if it
    ///     is already going; a looping one is left alone unless it has stopped.
    /// </summary>
    private static Guid PlaySingleInstance(SoundEffectInstanceRecord record, float volume, bool looped)
    {
        if (record.SoundInstance == null)
        {
            return Guid.Empty;
        }

        // If the sound is currently playing or paused, reset it
        if (!looped && record.SoundInstance.State is SoundState.Playing or SoundState.Paused)
        {
            record.SoundInstance.Stop(true);
        }

        // Play the sound
        record.SoundInstance.Volume = volume;

        if (!looped || record.SoundInstance.State is SoundState.Stopped or SoundState.Paused)
        {
            record.SoundInstance.Play();
        }

        return record.Id;
    }

    /// <summary>
    ///     Plays a sound that may overlap with itself, on a new instance if there is room for one and on
    ///     the oldest of the existing ones if there is not.
    /// </summary>
    private Guid PlayMultiInstance(string soundName, float volume)
    {
        // Create a new instance, but maybe keep a list of all instances created so we can put a max seal of the number of instances per sound allowed
        if (!_multiInstanceSoundEffects.TryGetValue(soundName, out var misxf) || misxf == null)
        {
            return Guid.Empty;
        }

        var playing = new List<SoundEffectInstanceRecord>();

        foreach (var t in _multiInstanceSoundEffectInstances)
        {
            if (t.SoundName == soundName)
            {
                playing.Add(t);
            }
        }

        var soundToPlay = playing.Count > 1 && playing.Count >= misxf.MaxInstances
            ? StealLongestPlaying(playing)
            : CreateNewInstance(soundName, misxf);

        soundToPlay.SoundInstance.Volume = volume;
        soundToPlay.SoundInstance.Play();

        // Return the ID of the multisound
        return soundToPlay.Id;
    }

    /// <summary>
    ///     Takes over the instance that has been playing longest, which is the one a listener is least
    ///     likely to notice being cut off.
    /// </summary>
    private static SoundEffectInstanceRecord StealLongestPlaying(List<SoundEffectInstanceRecord> playing)
    {
        var soundToPlay = playing[0];

        for (var a = 1; a < playing.Count; a++)
        {
            if (playing[a].PlayStart < soundToPlay.PlayStart)
            {
                soundToPlay = playing[a];
            }
        }

        soundToPlay.SoundInstance.Stop();

        // Update the playtime - this should update the master list automatically since we have kept the same reference
        soundToPlay.PlayStart = DateTime.Now.Ticks;

        return soundToPlay;
    }

    private SoundEffectInstanceRecord CreateNewInstance(string soundName, MultiInstanceSoundEffect misxf)
    {
        var soundToPlay = new SoundEffectInstanceRecord(
            soundName,
            SoundEffectInstanceType.Multi,
            misxf.Sound.CreateInstance(),
            DateTime.Now.Ticks,
            0,
            misxf.KillOnScreenChange)
        {
            // Set the ID of the sound we're playing
            Id = Guid.NewGuid()
        };

        // Record the instance
        _multiInstanceSoundEffectInstances.Add(soundToPlay);

        return soundToPlay;
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