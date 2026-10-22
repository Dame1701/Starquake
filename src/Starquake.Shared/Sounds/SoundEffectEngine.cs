// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Starquake.Constants;
using Starquake.Core;

namespace Starquake.Sounds;

internal class SoundEffectEngine(ContentManager content) : IDisposable
{
    /// <summary>
    ///     How long the background rumble waits before its first outing once switched on, and between
    ///     outings after that. A range, drawn from at random each time, so it never falls into a
    ///     rhythm.
    /// </summary>
    private const int FirstSoundMinDelayMs = 1000;

    /// <inheritdoc cref="FirstSoundMinDelayMs" />
    private const int FirstSoundMaxDelayMs = 10000;

    /// <inheritdoc cref="FirstSoundMinDelayMs" />
    private const int NextSoundMinDelayMs = 10000;

    /// <inheritdoc cref="FirstSoundMinDelayMs" />
    private const int NextSoundMaxDelayMs = 20000;

    /// <summary>How loud the background rumble is against everything else.</summary>
    private const float PeriodicSoundVolume = 0.5f;

    private readonly List<SoundEffectInstanceRecord> _multiInstanceSoundEffectInstances = [];
    private readonly Dictionary<string, MultiInstanceSoundEffect> _multiInstanceSoundEffects = new();

    /// <summary>
    ///     Guards everything to do with the periodic sound timer. Its callback runs on a thread pool
    ///     thread and re-arms the timer from there, so enabling, disabling and disposing must all
    ///     agree with the callback about whether the timer is still wanted.
    /// </summary>
    private readonly Lock _periodicSoundLock = new();

    private readonly List<SoundEffectInstanceRecord> _singleInstanceSoundEffectInstances = [];
    private bool _periodicBackgroundSoundsEnabled;
    private Guid _periodicBackgroundSoundsId = Guid.Empty;

    /// <summary>
    ///     Set by the timer callback and cleared by <see cref="Update" />. Setting this is the whole
    ///     of what the callback may do to this engine. See <see cref="PlayPeriodicSound" />.
    /// </summary>
    private int _periodicSoundDue;

    /// <summary>
    ///     One timer for the life of the engine, re-armed rather than replaced. Creating a timer per
    ///     enable lets the callback arm a fresh one just after the old is disposed, giving a rumble
    ///     that will not stop and can fire at content already unloaded on the way out.
    /// </summary>
    private Timer? _periodicSoundTimer;

    /// <summary>
    ///     Stops the background rumble for good. The sound effect instances belong to the content
    ///     manager, which disposes them with everything else. The timer is the one thing here that
    ///     outlives the game if not put down, and it would call into those instances after they go.
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
    ///     either way: skipping it while the sound is still playing silences the rumble for the rest
    ///     of the game.
    /// </summary>
    /// <remarks>
    ///     Runs on a thread pool thread, so it plays nothing itself. Calling <c>PlaySound</c> or
    ///     <c>GetSoundState</c> here would walk the instance lists that the game thread rewrites in
    ///     <c>KillSounds</c> on every screen change. <see cref="Update" /> does the playing, on the
    ///     game thread.
    /// </remarks>
    private void PlayPeriodicSound(object? state)
    {
        Interlocked.Exchange(ref _periodicSoundDue, 1);

        lock (_periodicSoundLock)
        {
            // Disabled or disposed while this callback was running, so it is not wanted again.
            if (!_periodicBackgroundSoundsEnabled)
            {
                return;
            }
            _periodicSoundTimer?.Change(Random.Shared.Next(NextSoundMinDelayMs, NextSoundMaxDelayMs), Timeout.Infinite);
        }
    }

    /// <summary>
    ///     Plays the background rumble if the timer has asked for it since the last frame, unless the
    ///     last one is still going. Called once a frame from the game thread, the only thread allowed
    ///     to touch the sound effect instances.
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
    ///     Stops what is playing. Pausing takes everything down. A screen change takes down only the
    ///     sounds belonging to the screen they started on, so one meant to carry across, such as the
    ///     machinery hum or the background rumble, keeps playing.
    /// </summary>
    public void KillSounds(bool paused)
    {
        StopPlayingInstances(_singleInstanceSoundEffectInstances, paused);
        StopPlayingInstances(_multiInstanceSoundEffectInstances, paused);
    }

    /// <summary>
    ///     Stops every instance in one of the two lists, or only those that do not survive a screen
    ///     change.
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

    /// <summary>
    ///     Sets how far off true a sound that may only play once at a time plays, and which speaker
    ///     it is in. Does nothing to any other sound. The volume is left to <c>PlaySound</c> and
    ///     <see cref="StopSound" />, which keep their own account of it for fading.
    /// </summary>
    /// <param name="id">The sound, as <c>PlaySound</c> returned it.</param>
    /// <param name="pitch">Minus one an octave down, one an octave up.</param>
    /// <param name="pan">Minus one for the left speaker, one for the right.</param>
    public void Bend(Guid id, float pitch, float pan)
    {
        SoundEffectInstanceRecord? record = _singleInstanceSoundEffectInstances.FirstOrDefault(sfx => sfx.Id == id);
        if (record == null)
        {
            return;
        }

        record.SoundInstance.Pitch = pitch;
        record.SoundInstance.Pan = pan;
    }

    public Guid PlaySound(string soundName, bool looped) => PlaySound(soundName, 1F, looped);

    public Guid PlaySound(string soundName, float volume) => PlaySound(soundName, volume, false);

    public Guid PlaySound(string soundName, float volume, bool looped)
    {
        SoundEffectInstanceRecord? singleInstance = FindSingleInstance(soundName);
        if (singleInstance == null)
        {
            return PlayMultiInstance(soundName, volume);
        }
        singleInstance.Volume = volume;
        return PlaySingleInstance(singleInstance, volume, looped);
    }

    /// <summary>
    ///     The instance reserved for a sound that may only play once at a time, or null if this sound
    ///     is not one of those.
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
    ///     Plays the sound on its own reserved instance. A one-shot restarts from the beginning if it
    ///     is already going; a looping one is left alone unless it has stopped.
    /// </summary>
    private static Guid PlaySingleInstance(SoundEffectInstanceRecord record, float volume, bool looped)
    {
        // A one-shot asked for again starts over. A looped one is left alone, so asking for it every
        // frame does not restart it every frame.
        if (!looped && record.SoundInstance.State is SoundState.Playing or SoundState.Paused)
        {
            record.SoundInstance.Stop(true);
        }

        record.SoundInstance.Volume = volume;

        if (!looped || record.SoundInstance.State is SoundState.Stopped or SoundState.Paused)
        {
            record.SoundInstance.Play();
        }
        return record.Id;
    }

    /// <summary>
    ///     Plays a sound that may overlap with itself: on a new instance if there is room, and on the
    ///     oldest existing one if not.
    /// </summary>
    private Guid PlayMultiInstance(string soundName, float volume)
    {
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

        return soundToPlay.Id;
    }

    /// <summary>
    ///     Takes over the instance playing longest, which a listener is least likely to notice being
    ///     cut off.
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

        // The record is the one in the master list, so writing through this reference keeps the
        // list's idea of the oldest instance up to date.
        //
        // Environment.TickCount64 rather than DateTime.Now, because this only answers which of two
        // instances started first. A monotonic counter does that without a timezone conversion and
        // without a clock step reordering them.
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
            Id = Guid.NewGuid()
        };

        _multiInstanceSoundEffectInstances.Add(soundToPlay);

        return soundToPlay;
    }

    public Guid PlaySound(string soundName) => PlaySound(soundName, 1, false);

    private SoundState GetSoundState(Guid id)
    {
        SoundEffectInstanceRecord? soundEffectInstanceRecord = _singleInstanceSoundEffectInstances
            .FirstOrDefault(sfx => sfx.Id == id);

        return soundEffectInstanceRecord != null
            ? soundEffectInstanceRecord.SoundInstance.State
            : SoundState.Stopped;
    }
}
