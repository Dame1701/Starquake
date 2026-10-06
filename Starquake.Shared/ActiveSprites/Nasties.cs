// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Helpers;
using Starquake.Input;
using Starquake.Particles;
using Starquake.SharedLibs;
using Starquake.Structure;
using Starquake.World;

namespace Starquake.ActiveSprites;

// This wraps multiple nasties
internal class Nasties
{

    /// <summary>
    ///     A nasty parked this far off screen is one that has not been given a home yet, so it is
    ///     skipped rather than drawn or tested.
    /// </summary>
    private const int UnplacedNastyPosition = -1000;

    /// <summary>
    ///     The size of Blob's shot, for testing it against a nasty.
    /// </summary>
    private const int FireWidth = TileMetrics.CollisionCellWidth;

    /// <inheritdoc cref="FireWidth" />
    private const int FireHeight = TileMetrics.CollisionCellHeight;

    /// <summary>
    ///     The reach of an electricity node's arc, which spans a whole tile across but only a
    ///     collision cell down.
    /// </summary>
    private const int ElectricityArcWidth = TileMetrics.TileWidth;

    /// <inheritdoc cref="ElectricityArcWidth" />
    private const int ElectricityArcHeight = TileMetrics.CollisionCellHeight;

    /// <summary>
    ///     One screen in this many is populated with a single kind of nasty instead of a mixture.
    /// </summary>
    private const int SameNastyScreenOdds = 4;
    private readonly Blob _blob;
    private readonly ConveyorNasties _conveyorNasties;
    private readonly ElectricityGatesAndNodesEngine _electricityNodeGates;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly ToggleKeys _input;

    private readonly List<Nasty> _nasties;
    private readonly int _nastyCount;
    private readonly IPositionalTriggerParticleEffect _nastySmokes;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;
    private int _previousScreenVisited;

    /// <summary>
    ///     Told when one of these blows itself apart, and handed on to each of them. Only the
    ///     mechanical ones ever raise it.
    /// </summary>
    private readonly Action _onNastyExploded;

    public Nasties(
        ResourceManager resources,
        GameScreenCollection gameScreens,
        GraphicsSet graphicsSet,
        Blob blob,
        Random randomNumberGenerator,
        ConveyorNasties conveyorNasties,
        ElectricityGatesAndNodesEngine electricityNodeGates,
        Action onNastyExploded,
        int nastyCount,
        ToggleKeys input)
    {
        _onNastyExploded = onNastyExploded;
        _resources = resources;
        _gameScreens = gameScreens;
        _graphicsSet = graphicsSet;
        _blob = blob;
        _randomNumberGenerator = randomNumberGenerator;
        _conveyorNasties = conveyorNasties;
        _electricityNodeGates = electricityNodeGates;
        _input = input;

        _nastyCount = nastyCount;
        _nasties = new List<Nasty>();
        _nastySmokes =
            (IPositionalTriggerParticleEffect)_resources.GetParticleEffectInstance(Assets.Effects.BasicSmokePlume,
                ParticleEffectType.TriggeredMultiLocationTimed, null);

    }
    public event RaiseInteractionWithBlob? OnNastyInteractionWithBlob;

    public void LoadContent()
    {
        // Twice as many as a screen holds, so the ones being left behind can stay as they are while
        // the next screen's are set up.
        _nasties.Clear();
        for (int a = 0; a < _nastyCount * 2; a++)
        {
            _nasties.Add(
                new Nasty(
                    _resources,
                    _gameScreens,
                    _graphicsSet,
                    _blob,
                    _randomNumberGenerator,
                    _onNastyExploded,
                    0,
                    0,
                    3));
        }
    }

    public void Reset()
    {
        for (int a = 0; a < _nastyCount; a++)
        {
            _nasties[a].AwaitFirstAppearance();
        }

        PerformPostChangeScreenActions();
    }

    public void Update(GameTime gameTime)
    {
        if (_blob.BlobsState != BlobState.Normal)
        {
            Reset();
            return;
        }

        // Cleared each frame and set again by whichever nasty is touching him, so letting go of Blob
        // needs no event of its own.
        _blob.BlobTouchedByNasty = false;

        if (_input.KeyboardState.IsKeyDown(Keys.R))
        {
            for (int a = 0; a < _nastyCount; a++)
            {
                _nasties[a].NastyState = NastyState.Dead;
            }
        }

        // Only the first nasty touching Blob drains his energy - all of them at once is too quick
        // and unfair
        bool nastyInteractingWithBlob = false;
        int nastySpawnedCounter = 0;

        for (int a = 0; a < _nastyCount; a++)
        {
            if (_nasties[a].NastyState == NastyState.Normal)
            {
                nastySpawnedCounter++;
            }

            // A nasty parked off the map has not been given a place on this screen, so there is
            // nothing to update - see UnplacedNastyPosition.
            if (_nasties[a].XPos <= UnplacedNastyPosition ||
                _nasties[a].YPos <= UnplacedNastyPosition)
            {
                continue;
            }

            _nasties[a].Update(gameTime);

            TouchBlob(a, ref nastyInteractingWithBlob);
            TakeAnyFireHit(a);
            ElectrifyOnGateContact(a);
        }

        if (nastySpawnedCounter == _nastyCount)
        {
            _resources.GetSoundFxEngine().EnablePeriodicBackgroundSounds();
        }

        _nastySmokes.Update(gameTime);
    }

    /// <summary>
    ///     Blob's box is pulled in by eight pixels all round so that a near miss is not counted as a
    ///     touch. Only the first nasty to make contact in a frame raises the event.
    /// </summary>
    private void TouchBlob(int a, ref bool nastyInteractingWithBlob)
    {
        if (CollisionDetection.Check(
                _blob.BlobXPos + 8,
                _blob.BlobYPos + 8,
                _blob.BlobWidth - 16,
                _blob.BlobHeight - 16,
                _nasties[a].XPos,
                _nasties[a].YPos,
                _nasties[a].NastyWidth,
                _nasties[a].NastyHeight))
        {
            if (_nasties[a].NastyState == NastyState.Normal)
            {
                if (OnNastyInteractionWithBlob != null)
                {
                    if (!nastyInteractingWithBlob)
                    {
                        nastyInteractingWithBlob = true;
                        OnNastyInteractionWithBlob(NastyInteractionType.HitBlob, _nasties[a].NastyType);
                    }
                }

                _blob.BlobTouchedByNasty = true;
            }
        }
    }

    private void TakeAnyFireHit(int a)
    {
        if (_blob.Weapon.HasFired)
        {
            if (CollisionDetection.Check(
                    _blob.Weapon.X,
                    _blob.Weapon.Y,
                    FireWidth,
                    FireHeight,
                    _nasties[a].XPos,
                    _nasties[a].YPos,
                    _nasties[a].NastyWidth,
                    _nasties[a].NastyHeight))
            {
                if (_nasties[a].NastyState == NastyState.Normal ||
                    _nasties[a].NastyState == NastyState.Birth)
                {
                    _blob.Weapon.RegisterHit();
                    _nasties[a].NastyState = NastyState.Shot;

                    if (_nasties[a].NastyType == NastyType.Spiky)
                    {
                        _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.BlobExplodes);
                    }
                    else
                    {
                        _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.NastyDeath);
                    }

                    if (OnNastyInteractionWithBlob != null)
                    {
                        OnNastyInteractionWithBlob(NastyInteractionType.BlobShot,
                            _nasties[a].NastyType);
                    }
                }
            }
        }
    }

    private void ElectrifyOnGateContact(int a)
    {
        _nasties[a].Smoking = false;

        if (_electricityNodeGates.ElectricityNodeControllers != null)
        {
            for (int b = 0;
                 b < _electricityNodeGates.ElectricityNodeControllers.Length;
                 b++)
            {
                if (_electricityNodeGates.ElectricityNodeControllers[b].ElectricityOn)
                {
                    if (CollisionDetection.Check(
                            _electricityNodeGates.ElectricityNodeControllers[b].Location
                                .X,
                            _electricityNodeGates.ElectricityNodeControllers[b].Location
                                .Y,
                            ElectricityArcWidth,
                            ElectricityArcHeight,
                            _nasties[a].XPos,
                            _nasties[a].YPos,
                            _nasties[a].NastyWidth,
                            _nasties[a].NastyHeight))
                    {
                        Direction nastyTravelDirection = CollisionDetection.CheckCollisionDirection(
                            _nasties[a].Speed,
                            _nasties[a].XPos,
                            _nasties[a].YPos,
                            _nasties[a].NastyWidth,
                            _nasties[a].NastyHeight,
                            _electricityNodeGates.ElectricityNodeControllers[b].Location
                                .X,
                            _electricityNodeGates.ElectricityNodeControllers[b].Location
                                .Y,
                            ElectricityArcWidth,
                            ElectricityArcHeight);

                        if (nastyTravelDirection != Direction.None &&
                            _nasties[a].NastyState == NastyState.Normal)
                        {
                            _nasties[a].Smoking = true;
                            _nastySmokes.Start(
                                SpriteHelpers.GetSpriteLocationOffset(_nasties[a].Direction,
                                    _nasties[a].NastyWidth, _nasties[a].NastyHeight).X +
                                _nasties[a].XPos,
                                SpriteHelpers.GetSpriteLocationOffset(_nasties[a].Direction,
                                    _nasties[a].NastyWidth, _nasties[a].NastyHeight).Y +
                                _nasties[a].YPos);
                        }
                    }
                }
            }
        }
    }

    private void Swap(List<Nasty> list, int a, int b)
    {
        (list[a], list[b]) = (list[b], list[a]);
    }

    /// <summary>
    ///     Hands the screen's nasties over on a screen change. Walking back the way you came gets the
    ///     same nasties you left, kept in the second half of the array; going anywhere else gets a
    ///     fresh batch and puts the ones you left behind into that half.
    /// </summary>
    public void PerformPostChangeScreenActions()
    {
        bool goingBackTheWayWeCame = _gameScreens.ScreenNumber == _previousScreenVisited;

        if (goingBackTheWayWeCame)
        {
            for (int a = 0; a < _nastyCount; a++)
            {
                Swap(_nasties, a, a + _nastyCount);
            }
        }
        else
        {
            StoreNastiesFromScreenWeLeft();
            CreateFreshNasties();
        }

        for (int a = 0; a < _nastyCount; a++)
        {
            _nasties[a].PerformPostChangeScreenActions();
        }

        DisableNastiesDisplacedByConveyorNasties();

        _nastySmokes.Reset();

        _previousScreenVisited = _gameScreens.PreviousScreenNumber;
    }

    /// <summary>
    ///     Saves the nasties of the screen being left into the second half of the array, where they
    ///     wait in case Blob comes straight back. Any that were dead are revived first, so a screen
    ///     is never returned to empty.
    /// </summary>
    private void StoreNastiesFromScreenWeLeft()
    {
        for (int a = 0; a < _nastyCount; a++)
        {
            if (_nasties[a].NastyState is NastyState.Dead or NastyState.Shot)
            {
                // The short wait, not the long one a death would take: this is a screen being put
                // away, and Blob walking back into it should find it populated rather than find
                // the nasty he killed still serving out its ten seconds.
                _nasties[a].AwaitFirstAppearance();
            }

            _nasties[a + _nastyCount].CopyFromSource(_nasties[a]);
        }
    }

    /// <summary>
    ///     A new screen's worth of nasties. One screen in four is populated with a single kind rather
    ///     than a mixture, which is what makes some rooms feel like they belong to something.
    /// </summary>
    private void CreateFreshNasties()
    {
        GameScreenData screenData = _gameScreens.CurrentGameScreenData;

        screenData.KeepNastiesTheSame =
            _randomNumberGenerator.Next(0, SameNastyScreenOdds) == 0;

        NastyType nastyType = screenData.KeepNastiesTheSame
            ? Nasty.GetRandomNastyType(_randomNumberGenerator)
            : NastyType.Ghost;

        for (int a = 0; a < _nastyCount; a++)
        {
            _nasties[a].KeepNastyTheSame = screenData.KeepNastiesTheSame;
            _nasties[a].AwaitFirstAppearance();

            if (screenData.KeepNastiesTheSame)
            {
                _nasties[a].NastyType = nastyType;
            }
        }
    }

    /// <summary>
    ///     A conveyor nasty takes the place of an ordinary one rather than adding to the count, so the
    ///     first few are switched off.
    /// </summary>
    private void DisableNastiesDisplacedByConveyorNasties()
    {
        for (int a = 0; a < _conveyorNasties.NastiesActive; a++)
        {
            _nasties[a].NastyState = NastyState.Disabled;
        }
    }

    public void Draw(int screenFade)
    {
        if (_blob.BlobsState == BlobState.Normal)
        {
            for (int a = 0; a < _nastyCount; a++)
            {
                if (_nasties[a].XPos > UnplacedNastyPosition &&
                    _nasties[a].YPos > UnplacedNastyPosition)
                {
                    _nasties[a].Draw(screenFade);
                }
            }

            _nastySmokes.Draw(screenFade);
        }
    }
}
