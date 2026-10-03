// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Structure;

namespace Starquake.Sounds;

internal class SoundEffectEngine(ContentManager content) : IDisposable
{
    /// <summary>
    ///     How long the background rumble waits before its first outing once it is switched on, and
    ///     between outings after that. A range, picked from at random each time, so the rumble does
    ///     not fall into a rhythm.
    /// </summary>
    private const int FirstSoundMinDelayMs = 1000;

    /// <inheritdoc cref="FirstSoundMinDelayMs" />
    private const int FirstSoundMaxDelayMs = 10000;

    /// <inheritdoc cref="FirstSoundMinDelayMs" />
    private const int NextSoundMinDelayMs = 10000;

    /// <inheritdoc cref="FirstSoundMinDelayMs" />
    private const int NextSoundMaxDelayMs = 20000;

    /// <summary>
    ///     How loud the background rumble is against everything else.
    /// </summary>
    private const float PeriodicSoundVolume = 0.5f;

    private readonly List<SoundEffectInstanceRecord> _multiInstanceSoundEffectInstances = [];
    private readonly Dictionary<string, MultiInstanceSoundEffect> _multiInstanceSoundEffects = new();

    /// <summary>
    ///     Guards everything to do with the periodic sound timer. Its callback runs on a thread pool
    ///     thread and re-arms the timer from there, so enabling, disabling and disposing all have to
    ///     agree with the callback about whether the timer is still wanted.
    /// </summary>
    private readonly Lock _periodicSoundLock = new();

    private readonly List<SoundEffectInstanceRecord> _singleInstanceSoundEffectInstances = [];
    private bool _periodicBackgroundSoundsEnabled;
    private Guid _periodicBackgroundSoundsId = Guid.Empty;

    /// <summary>
    ///     Set by the timer callback and cleared by <see cref="Update" />, which is the whole of what
    ///     the callback is allowed to do to this engine. See the note on <see cref="PlayPeriodicSound" />.
    /// </summary>
    private int _periodicSoundDue;

    /// <summary>
    ///     One timer for the life of the engine, re-armed rather than replaced. Enabling and
    ///     disabling the background rumble used to create and throw away a timer each time, which
    ///     left the callback able to arm a fresh one just after the old one was disposed - a rumble
    ///     that would not stop, and one still able to fire at content that had already been
    ///     unloaded on the way out of the game.
    /// </summary>
    private Timer? _periodicSoundTimer;

    /// <summary>
    ///     Stops the background rumble for good. The sound effect instances themselves belong to the
    ///     content manager, which disposes them with the rest of the content; the timer is the one
    ///     thing here that outlives the game if it is not put down, and it would be calling into
    ///     those instances after they had gone.
    /// </summary>
    public void Dispose()
    {
        lock (_periodicSoundLock)
        {
            _periodicBackgroundSoundsEnabled = false;

            _periodicSoundTimer?.Dispose();
            _periodicSoundTimer = null;
        }
    }

    public void LoadSoundEffect(SoundDescription soundDesc)
    {
        switch (soundDesc.InstanceType)
        {
            case SoundEffectInstanceType.Single:
            {
                var sfx = content.Load<SoundEffect>(soundDesc.FilePath);
                var soundEffectInstanceRecord = new SoundEffectInstanceRecord(soundDesc.ResourceName,
                    sfx.CreateInstance(), 0, 0, soundDesc.KillOnScreenChange)
                {
                    Id = Guid.NewGuid()
                };

                _singleInstanceSoundEffectInstances.Add(soundEffectInstanceRecord);
                break;
            }
            case SoundEffectInstanceType.Multi:
                _multiInstanceSoundEffects.Add(soundDesc.ResourceName,
                    new MultiInstanceSoundEffect(content.Load<SoundEffect>(soundDesc.FilePath),
                        soundDesc.MaxInstances,
                        soundDesc.KillOnScreenChange));
                break;
        }
    }

    public void EnablePeriodicBackgroundSounds()
    {
        lock (_periodicSoundLock)
        {
            if (_periodicBackgroundSoundsEnabled)
            {
                return;
            }

            _periodicBackgroundSoundsEnabled = true;
            _periodicSoundTimer ??= new Timer(PlayPeriodicSound, null, Timeout.Infinite, Timeout.Infinite);
            _periodicSoundTimer.Change(Random.Shared.Next(FirstSoundMinDelayMs, FirstSoundMaxDelayMs), Timeout.Infinite);
        }
    }

    public void DisablePeriodicBackgroundSounds(bool killPlayingSounds = false)
    {
        lock (_periodicSoundLock)
        {
            if (!_periodicBackgroundSoundsEnabled)
            {
                return;
            }

            _periodicBackgroundSoundsEnabled = false;
            _periodicSoundTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        if (killPlayingSounds)
        {
            StopSound(_periodicBackgroundSoundsId, SoundFade.None);
        }
    }

    /// <summary>
    ///     Notes that the background rumble is due and asks for the next one. The asking happens
    ///     either way: skipping it when the sound was still playing is what used to silence the
    ///     rumble for the rest of the game the first time the timer caught it.
    /// </summary>
    /// <remarks>
    ///     This runs on a thread pool thread, so it plays nothing itself. It used to call PlaySound
    ///     and GetSoundState from here, both of which walk the instance lists that the game thread
    ///     rewrites in KillSounds on every screen change. That survived only because
    ///     "BackgroundSound1" is registered as a single-instance sound: change that one line of
    ///     ContentManifest to Multi and the callback starts calling List.Add on a list the game
    ///     thread is enumerating. <see cref="Update" /> does the playing, on the game thread.
    /// </remarks>
    private void PlayPeriodicSound(object? state)
    {
        Interlocked.Exchange(ref _periodicSoundDue, 1);

        lock (_periodicSoundLock)
        {
            // Disabled or disposed while this callback was running - it is not wanted again.
            if (!_periodicBackgroundSoundsEnabled)
            {
                return;
            }

            _periodicSoundTimer?.Change(Random.Shared.Next(NextSoundMinDelayMs, NextSoundMaxDelayMs), Timeout.Infinite);
        }
    }

    /// <summary>
    ///     Plays the background rumble if the timer has asked for it since the last frame, unless the
    ///     last one is still going. Called once a frame from the game thread, which is the only thread
    ///     allowed to touch the sound effect instances.
    /// </summary>
    public void Update()
    {
        if (Interlocked.Exchange(ref _periodicSoundDue, 0) == 0)
        {
            return;
        }

        if (GetSoundState(_periodicBackgroundSoundsId) != SoundState.Playing)
        {
            _periodicBackgroundSoundsId = PlaySound(Assets.Sounds.BackgroundSound1, PeriodicSoundVolume, false);
        }
    }

    /// <summary>
    ///     Stops what is playing. Pausing takes everything down; a screen change takes down only the
    ///     sounds that said they belong to the screen they were started on, so a sound meant to carry
    ///     across one - the machinery hum, the background rumble - keeps playing.
    /// </summary>
    public void KillSounds(bool paused)
    {
        StopPlayingInstances(_singleInstanceSoundEffectInstances, paused);
        StopPlayingInstances(_multiInstanceSoundEffectInstances, paused);
    }

    /// <summary>
    ///     Stops every instance in one of the two lists, or only the ones that do not survive a screen
    ///     change. Both lists hold the same record type and are stopped the same way, so the two of
    ///     them times the two reasons used to be four copies of this loop.
    /// </summary>
    private static void StopPlayingInstances(
        IEnumerable<SoundEffectInstanceRecord> instances,
        bool includingSoundsThatSurviveAScreenChange)
    {
        foreach (SoundEffectInstanceRecord instance in instances)
        {
            if (!includingSoundsThatSurviveAScreenChange && !instance.KillOnScreenChange)
            {
                continue;
            }

            if (instance.SoundInstance.State != SoundState.Stopped)
            {
                instance.SoundInstance.Stop();
            }
        }
    }

    public void StopSound(Guid id, SoundFade soundFade)
    {
        SoundEffectInstanceRecord? soundEffectInstanceRecord = _singleInstanceSoundEffectInstances.FirstOrDefault(sfx => sfx.Id == id);
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

        foreach (SoundEffectInstanceRecord t in _multiInstanceSoundEffectInstances)
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
        SoundEffectInstanceRecord? singleInstance = FindSingleInstance(soundName);

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
        foreach (SoundEffectInstanceRecord t in _singleInstanceSoundEffectInstances)
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
        if (!_multiInstanceSoundEffects.TryGetValue(soundName, out MultiInstanceSoundEffect? multiInstanceSound))
        {
            return Guid.Empty;
        }

        var playing = new List<SoundEffectInstanceRecord>();

        foreach (SoundEffectInstanceRecord t in _multiInstanceSoundEffectInstances)
        {
            if (t.SoundName == soundName)
            {
                playing.Add(t);
            }
        }

        SoundEffectInstanceRecord soundToPlay = playing.Count > 1 && playing.Count >= multiInstanceSound.MaxInstances
            ? StealLongestPlaying(playing)
            : CreateNewInstance(soundName, multiInstanceSound);

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
        SoundEffectInstanceRecord soundToPlay = playing[0];

        for (int a = 1; a < playing.Count; a++)
        {
            if (playing[a].PlayStart < soundToPlay.PlayStart)
            {
                soundToPlay = playing[a];
            }
        }

        soundToPlay.SoundInstance.Stop();

        // Update the playtime - this should update the master list automatically since we have kept the same reference.
        // Environment.TickCount64 rather than DateTime.Now: this only ever answers which of two
        // instances started first, and a monotonic counter does that without a timezone conversion
        // and without a clock step being able to reorder them.
        soundToPlay.PlayStart = Environment.TickCount64;

        return soundToPlay;
    }

    private SoundEffectInstanceRecord CreateNewInstance(string soundName, MultiInstanceSoundEffect multiInstanceSound)
    {
        var soundToPlay = new SoundEffectInstanceRecord(
            soundName,
            multiInstanceSound.Sound.CreateInstance(),
            Environment.TickCount64,
            0,
            multiInstanceSound.KillOnScreenChange)
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
        SoundEffectInstanceRecord? soundEffectInstanceRecord = _singleInstanceSoundEffectInstances
            .FirstOrDefault(sfx => sfx.Id == id);

        return soundEffectInstanceRecord != null ? soundEffectInstanceRecord.SoundInstance.State : SoundState.Stopped;
    }
}
