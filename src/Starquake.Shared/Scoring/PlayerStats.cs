// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Constants;
using Starquake.Core;
using Starquake.Sounds;

namespace Starquake.Scoring;

/// <summary>
///     What the player has: score, lives, and the three supplies he spends. Every rule that changes
///     one of them lives here - what each power-up is worth, which supply the wildcard tops up, what
///     losing a life costs and gives back, and how fast energy drains while he is walking about.
/// </summary>
/// <remarks>
///     Nothing here draws, so nothing needs a graphics device. The sound engine is the only
///     collaborator, and it answers harmlessly for a sound it has never been given, which keeps the
///     scoring and power-up rules reachable from a test.
///     <para>
///         The two methods deciding Blob's fate, <see cref="DrainEnergy" /> and
///         <see cref="TakeALife" />, return what they decided rather than acting on Blob, so this
///         class does not need to know what Blob is.
///     </para>
/// </remarks>
internal class PlayerStats
{
    /// <summary>
    ///     How long Blob goes between losing a bar of energy, in logic frames. Four seconds.
    /// </summary>
    private const int EnergyDrainFrames = 4 * LogicClock.FramesPerSecond;

    /// <summary>
    ///     How much of each supply Blob can hold, and how much he starts a life with. The panel's bars
    ///     are drawn full at these, so a pick-up that would take a supply over its ceiling is capped.
    /// </summary>
    private const int MaxFirePower = 128;

    /// <inheritdoc cref="MaxFirePower" />
    private const int MaxEnergyLevel = 128;

    /// <inheritdoc cref="MaxFirePower" />
    private const int MaxPlatforms = 64;

    /// <inheritdoc cref="MaxFirePower" />
    private const int StartingPlatforms = 32;

    /// <summary>How many lives a game starts with.</summary>
    private const int StartingLives = 4;

    /// <summary>
    ///     The platforms Blob is given on coming back, if he died with fewer, so losing a life never
    ///     strands him somewhere he cannot climb out of.
    /// </summary>
    private const int PlatformsOnRevival = 10;

    /// <summary>
    ///     What each power-up is worth. Three sizes of each, plus a top-up that gives whichever supply
    ///     is running lowest and an extra life.
    /// </summary>
    private const int SmallTopUp = 32;

    /// <inheritdoc cref="SmallTopUp" />
    private const int MediumTopUp = 64;

    /// <inheritdoc cref="SmallTopUp" />
    private const int LargeTopUp = 96;

    /// <inheritdoc cref="SmallTopUp" />
    private const int FullTopUp = 128;

    /// <summary>
    ///     A platform costs twice what a shot does, so the wildcard power-up weighs the platform count
    ///     double when deciding which supply is lowest.
    /// </summary>
    private const int PlatformCost = 2;

    private readonly SoundEffectEngine _sounds;

    private int _energyDrainCounter;
    private bool _energyDrainStarted;
    private double _energyLevel;
    private double _firePower;
    private double _platforms;

    public PlayerStats(SoundEffectEngine sounds)
    {
        _sounds = sounds;
        Reset();
    }

    public int PlayerScore { get; set; }

    public int BlobLives { get; set; }

    public double FirePower
    {
        get => _firePower;
        set => _firePower = Clamp(value, MaxFirePower);
    }

    public double EnergyLevel
    {
        get => _energyLevel;
        set => _energyLevel = Clamp(value, MaxEnergyLevel);
    }

    public double Platforms
    {
        get => _platforms;
        set => _platforms = Clamp(value, MaxPlatforms);
    }

    public void Reset()
    {
        BlobLives = StartingLives;
        PlayerScore = 0;
        _firePower = MaxFirePower;
        _energyLevel = MaxEnergyLevel;
        _platforms = StartingPlatforms;
        _energyDrainCounter = 0;
        _energyDrainStarted = false;
    }

    public void BlobHasPerformedAnAction(BlobAction action, object? data)
    {
        switch (action)
        {
            case BlobAction.FiredLazer:
            {
                FirePower--;
                break;
            }
            case BlobAction.LaidPlatform:
            {
                Platforms--;
                break;
            }
            case BlobAction.GotPowerUp:
            {
                // The only action that carries anything, and what it carries is the power-up's type.
                if (data is not int powerUpType)
                {
                    throw new ArgumentException(
                        "A power up action needs the type of power up collected.", nameof(data));
                }
                CollectPowerUp(powerUpType);
                break;
            }
        }
    }

    /// <summary>One frame of the slow energy drain, and whether it has just run Blob out.</summary>
    /// <param name="draining">
    ///     Whether Blob is somewhere that drains him. He is safe inside a teleporter and on every
    ///     screen but the main game.
    /// </param>
    /// <returns>True if the energy has run out and Blob should be killed.</returns>
    public bool DrainEnergy(bool draining)
    {
        if (!draining)
        {
            return false;
        }

        if (!_energyDrainStarted)
        {
            _energyDrainCounter = 0;
            _energyDrainStarted = true;
        }
        else
        {
            _energyDrainCounter++;

            if (_energyDrainCounter > EnergyDrainFrames)
            {
                _energyDrainStarted = false;
                _energyLevel -= 1;
            }
        }

        return EnergyLevel <= 0;
    }

    /// <summary>Takes a life, and either revives Blob or ends the game.</summary>
    /// <returns>True if that was the last life.</returns>
    public bool TakeALife()
    {
        BlobLives -= 1;

        if (BlobLives < 0)
        {
            BlobLives = 0;

            return true;
        }

        EnergyLevel = MaxEnergyLevel;

        TopUpPlatforms();

        return false;
    }

    /// <summary>
    ///     Gives Blob enough platforms to carry on with, so a death with none left does not revive him
    ///     somewhere he cannot climb out of. Only tops up; it never takes any away.
    /// </summary>
    private void TopUpPlatforms()
    {
        if (Platforms <= PlatformsOnRevival)
        {
            Platforms = PlatformsOnRevival;
        }
    }

    private static double Clamp(double value, int maximum)
        => value < 0 ? 0 : value > maximum ? maximum : value;

    private void CollectPowerUp(int powerUpType)
    {
        switch ((PowerUp)powerUpType)
        {
            case PowerUp.EnergyMedium:
                TopUpEnergy(MediumTopUp);
                break;
            case PowerUp.EnergySmall:
                TopUpEnergy(SmallTopUp);
                break;
            case PowerUp.EnergyFull:
                TopUpEnergy(FullTopUp);
                break;
            case PowerUp.FirePowerMedium:
                TopUpFirePower(MediumTopUp);
                break;
            case PowerUp.FirePowerSmall:
                TopUpFirePower(SmallTopUp);
                break;
            case PowerUp.Platforms:
                TopUpPlatforms(SmallTopUp);
                break;
            case PowerUp.WhateverIsLowest:
                TopUpWhateverIsLowest();
                break;
            case PowerUp.ExtraLife:
                GiveExtraLife();
                break;
        }
    }

    /// <summary>
    ///     The wildcard power-up. A life if Blob has none left, otherwise whichever of his three
    ///     supplies is shortest. Platforms are compared at their energy cost rather than their count,
    ///     since one platform is worth several units of anything else.
    /// </summary>
    private void TopUpWhateverIsLowest()
    {
        if (BlobLives == 0)
        {
            GiveExtraLife();

            return;
        }

        double platformsWorth = Platforms * PlatformCost;

        if (platformsWorth <= EnergyLevel && platformsWorth <= FirePower)
        {
            TopUpPlatforms(SmallTopUp);
        }
        else if (FirePower <= platformsWorth && FirePower <= EnergyLevel)
        {
            TopUpFirePower(MediumTopUp);
        }
        else if (EnergyLevel <= platformsWorth && EnergyLevel <= FirePower)
        {
            TopUpEnergy(LargeTopUp);
        }
    }

    private void TopUpEnergy(int amount)
    {
        EnergyLevel += amount;
        _sounds.PlaySound(Assets.Sounds.PowerUp2, false);
    }

    private void TopUpFirePower(int amount)
    {
        FirePower += amount;
        _sounds.PlaySound(Assets.Sounds.PowerUp3, 0.5f, false);
    }

    private void TopUpPlatforms(int amount)
    {
        Platforms += amount;
        _sounds.PlaySound(Assets.Sounds.PowerUp1, false);
    }

    private void GiveExtraLife()
    {
        BlobLives++;
        _sounds.PlaySound(Assets.Sounds.LifePowerUp, false);
    }
}
