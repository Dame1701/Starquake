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
            // Longer interval if we have reset, probably because Blob has died so give the player chance to respond
            _counterTarget = _randomGenerator.Next(240, 300); // Random number between four seconds and a five seconds
        }
        else if (changedScreens)
        {
            // Fix the electricity time for new screen entry
            _counterTarget = 60;
        }
        else
        {
            _counterTarget = _randomGenerator.Next(60, 300); // Random number between one second and five seconds
        }
    }

    public void Reset()
    {
        ResetCounter(true, false);
        ElectricityOn = false;
    }

    public void Update()
    {
        _counter++;

        if (_counter > _counterTarget)
        {
            // flip the electricity
            ElectricityOn = !ElectricityOn;
            ResetCounter(false, false);
        }

        if (ElectricityOn)
        {
            if (!_electricitySoundPlayed)
            {
                // Play sound
                _soundId = _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.ElectricityNodes, 0.3F);

                _electricitySoundPlayed = true;
            }
        }
        else
        {
            // Stop sound
            if (_soundId != Guid.Empty)
            {
                _resources.GetSoundFxEngine().StopSound(_soundId, SoundFade.None);
            }

            _electricitySoundPlayed = false;
        }
    }
}
