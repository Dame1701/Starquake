// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Sounds;

namespace Starquake.ActiveSprites;

internal class ElectricityRandomiser
{
    /// <summary>
    ///     How long the arcs hold their state for, in logic frames. Deliberately left as raw frame
    ///     counts: they were chosen for a sixty frame loop and
    ///     <see cref="LogicClock.FramesPerSecond" /> is fifty, so restating them in seconds would
    ///     retime the hazard rather than describe it.
    /// </summary>
    private const int OnScreenEntryFrames = 60;

    /// <inheritdoc cref="OnScreenEntryFrames" />
    private const int AfterResetMinFrames = 240;

    /// <inheritdoc cref="OnScreenEntryFrames" />
    private const int AfterResetMaxFrames = 300;

    /// <summary>
    ///     How loud the arcs are. They run constantly, so they sit below everything that happens
    ///     once.
    /// </summary>
    private const float SoundVolume = 0.3f;

    private readonly Random _randomGenerator;
    private readonly ResourceManager _resources;
    private int _counter;
    private int _counterTarget;
    private bool _electricitySoundPlayed;

    private Guid _soundId;

    public ElectricityRandomiser(ResourceManager resources, Random randomNumberGenerator)
    {
        _resources = resources;
        _electricitySoundPlayed = false;
        _randomGenerator = randomNumberGenerator;

        if (_randomGenerator.Next(0, 2) == 0)
        {
            ElectricityOn = false;
        }
        else
        {
            ElectricityOn = true;
        }
        _counter = 0;
        ResetCounter(false, true);
    }

    public bool ElectricityOn { get; private set; }

    public Point Location { get; set; }

    private void ResetCounter(bool nodeReset, bool changedScreens)
    {
        _counter = 0;
        if (nodeReset)
        {
            // Longer after a reset, which usually follows a death, so the player has a moment to get
            // clear before the arcs start again.
            _counterTarget = _randomGenerator.Next(AfterResetMinFrames, AfterResetMaxFrames);
        }
        else if (changedScreens)
        {
            // Always the same on arrival, so a screen's arcs are in a known state when Blob walks
            // on rather than flipping the instant he does.
            _counterTarget = OnScreenEntryFrames;
        }
        else
        {
            _counterTarget = _randomGenerator.Next(OnScreenEntryFrames, AfterResetMaxFrames);
        }
    }

    public void Reset()
    {
        ResetCounter(true, false);
        ElectricityOn = false;
    }

    /// <summary>
    ///     Starts the arcing sound if it is not already going. One instance covers however many
    ///     nodes are on the screen, which is why it is started from the flag rather than per node.
    /// </summary>
    private void StartElectricitySound()
    {
        if (_electricitySoundPlayed)
        {
            return;
        }

        _soundId = _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.ElectricityNodes, SoundVolume);
        _electricitySoundPlayed = true;
    }

    /// <inheritdoc cref="StartElectricitySound" />
    private void StopElectricitySound()
    {
        if (_soundId != Guid.Empty)
        {
            _resources.GetSoundFxEngine().StopSound(_soundId, SoundFade.None);
        }

        _electricitySoundPlayed = false;
    }

    public void Update()
    {
        _counter++;

        if (_counter > _counterTarget)
        {
            ElectricityOn = !ElectricityOn;
            ResetCounter(false, false);
        }

        if (ElectricityOn)
        {
            StartElectricitySound();
        }
        else
        {
            StopElectricitySound();
        }
    }
}
