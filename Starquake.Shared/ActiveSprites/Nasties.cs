// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
#nullable enable
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Engines;
using Starquake.Helpers;
using Starquake.Particles;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

// This wraps multiple nasties
public class Nasties : GameComponentBase
{
    /// <summary>
    ///     A nasty parked this far off screen is one that has not been given a home yet, so it is
    ///     skipped rather than drawn or tested.
    /// </summary>
    private const int UnplacedNastyPosition = -1000;

    /// <summary>
    ///     The size of the blob's shot, for testing it against a nasty.
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

    private readonly List<Nasty> _nasties;
    private readonly int _nastyCount = 1;
    private readonly IPositionalTriggerParticleEffect _nastySmokes;
    private int _previousScreenVisited;

    public Nasties(GameEngine gameEngineReference, int nastyCount)
        : base(gameEngineReference)
    {
        _nastyCount = nastyCount;
        _nasties = new List<Nasty>();
        _nastySmokes =
            (IPositionalTriggerParticleEffect)GameEngine.ContentResourceManager.GetParticleEffectInstance("BasicSmokePlume",
                ParticleEffectType.TriggeredMultiLocationTimed, null);

    }
    public event RaiseInteractionWithBlob? OnNastyInteractionWithBlob;

    #region IGameComponent Members

    public void LoadContent(ContentList content)
    {
        // Create double the number needed for screen swap operations
        _nasties.Clear();
        for (var a = 0; a < _nastyCount * 2; a++)
        {
            _nasties.Add(
                new Nasty(
                    GameEngine,
                    0,
                    0,
                    3));
        }
    }

    public void Initialise()
    {
        for (var a = 0; a < _nastyCount * 2; a++)
        {
            _nasties[a].Initialise();
        }

        // Maybe we should call this on start of the game for all involved....?
        PerformPostChangeScreenActions();
    }

    public void Reset()
    {
        for (var a = 0; a < _nastyCount; a++)
        {
            _nasties[a].NastyState = NastyState.PreConception;
        }

        PerformPostChangeScreenActions();
    }

    public override void Update(GameTime gameTime)
    {
        if (GameEngine.Player.BlobsState == BlobState.Normal)
        {
            // Cancel any nasty collision effect
            GameEngine.Player.BlobTouchedByNasty = false;

            if (Keyboard.GetState().IsKeyDown(Keys.R))
            {
                for (var a = 0; a < _nastyCount; a++)
                {
                    _nasties[a].NastyState = NastyState.Dead;
                }
            }

            // Flag that's set if a nasty is touching Blob already - stops all of them zapping his energy at once which to too quick and unfair
            var nastyInteractingWithBlob = false;
            var nastySpawnedCounter = 0;

            for (var a = 0; a < _nastyCount; a++)
            {
                if (_nasties[a].NastyState == NastyState.Normal)
                {
                    nastySpawnedCounter++;
                }

                // Setting the coordinates to a setting less than 0 will suspect all updates
                if (_nasties[a].XPos > UnplacedNastyPosition &&
                    _nasties[a].YPos > UnplacedNastyPosition)
                {
                    _nasties[a].Update(gameTime);

                    // Check for a collision between a nasty and Blob and raise an event
                    // Since we have a reference to Blob directly, we can send the call to animate contact directly
                    // We raise the event to affect the status panel
                    // Going to move the offsets etc. in a bit so it's not too precise and unfair!
                    if (CollisionDetection.Check(
                            GameEngine.Player.BlobXPos + 8,
                            GameEngine.Player.BlobYPos + 8,
                            GameEngine.Player.BlobWidth - 16,
                            GameEngine.Player.BlobHeight - 16,
                            _nasties[a].XPos,
                            _nasties[a].YPos,
                            _nasties[a].NastyWidth,
                            _nasties[a].NastyHeight))
                    {
                        if (_nasties[a].NastyState == NastyState.Normal)
                        {
                            // Raise the event
                            if (OnNastyInteractionWithBlob != null)
                            {
                                if (!nastyInteractingWithBlob)
                                {
                                    nastyInteractingWithBlob = true;
                                    OnNastyInteractionWithBlob(NastyInteractionType.HitBlob, _nasties[a].NastyType);
                                }
                            }

                            // Trigger the blob animation
                            GameEngine.Player.BlobTouchedByNasty = true;
                        }
                    }

                    // Has blob shot a nasty
                    if (GameEngine.Player.BlobHasFired)
                    {
                        if (CollisionDetection.Check(
                                GameEngine.Player.BlobFireX,
                                GameEngine.Player.BlobFireY,
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
                                GameEngine.Player.RegisterFireHit();
                                _nasties[a].NastyState = NastyState.Shot;

                                if (_nasties[a].NastyType == NastyType.Spiky)
                                {
                                    GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobExplodes");
                                }
                                else
                                {
                                    GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("NastyDeath");
                                }

                                if (OnNastyInteractionWithBlob != null)
                                {
                                    OnNastyInteractionWithBlob(NastyInteractionType.BlobShot,
                                        _nasties[a].NastyType);
                                }
                            }
                        }
                    }

                    // Has the nasty hit an electricity gate...?
                    _nasties[a].Smoking = false;

                    if (GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers != null)
                    {
                        for (var b = 0;
                             b < GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers.Length;
                             b++)
                        {
                            if (GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers[b].ElectricityOn)
                            {
                                if (CollisionDetection.Check(
                                        GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers[b].Location
                                            .X,
                                        GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers[b].Location
                                            .Y,
                                        ElectricityArcWidth,
                                        ElectricityArcHeight,
                                        _nasties[a].XPos,
                                        _nasties[a].YPos,
                                        _nasties[a].NastyWidth,
                                        _nasties[a].NastyHeight))
                                {
                                    var nastyTravelDirection = CollisionDetection.CheckCollisionDirection(
                                        _nasties[a].Speed,
                                        _nasties[a].XPos,
                                        _nasties[a].YPos,
                                        _nasties[a].NastyWidth,
                                        _nasties[a].NastyHeight,
                                        GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers[b].Location
                                            .X,
                                        GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers[b].Location
                                            .Y,
                                        ElectricityArcWidth,
                                        ElectricityArcHeight);

                                    if (nastyTravelDirection != Direction.None &&
                                        _nasties[a].NastyState == NastyState.Normal)
                                    {
                                        if (_nastySmokes != null)
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
                }
            }

            if (nastySpawnedCounter == _nastyCount)
            {
                GameEngine.ContentResourceManager.GetSoundFxEngine().EnablePeriodicBackgroundSounds();
            }

            if (_nastySmokes != null)
            {
                _nastySmokes.Update(gameTime);
            }
        }
        else
        {
            Reset();
        }
    }

    private void Swap(List<Nasty> list, int a, int b)
    {
        (list[a], list[b]) = (list[b], list[a]);
    }

    public void PerformPostChangeScreenActions()
    {
        if (GameEngine.GameScreens.ScreenNumber != _previousScreenVisited)
        {
            // Save the nasties on the screen we have left into the previous screen array
            for (var a = 0; a < _nastyCount; a++)
            {
                // Make sure they don't stay dead when we leave
                if (_nasties[a].NastyState == NastyState.Dead ||
                    _nasties[a].NastyState == NastyState.Shot)
                {
                    _nasties[a].NastyState = NastyState.PreConception;
                }

                _nasties[a + _nastyCount].CopyFromSource(_nasties[a]);
            }
        }

        // Do we have nasties from the previous screen...?
        if (GameEngine.GameScreens.ScreenNumber == _previousScreenVisited)
        {
            // Swap them all around
            for (var a = 0; a < _nastyCount; a++)
            {
                Swap(_nasties, a, a + _nastyCount);
            }
        }

        // Nope so we are going to create a batch from scratch
        if (GameEngine.GameScreens.ScreenNumber != _previousScreenVisited)
        {
            // Do we generate all nasties the same
            if (GameEngine.RandomNumberGenerator.Next(0, 4) == 0)
            {
                GameEngine.GameScreens.CurrentGameScreenData.KeepNastiesTheSame = true;
            }
            else
            {
                GameEngine.GameScreens.CurrentGameScreenData.KeepNastiesTheSame = false;
            }

            var nastyType = NastyType.Ghost;
            if (GameEngine.GameScreens.CurrentGameScreenData.KeepNastiesTheSame)
            {
                nastyType = Nasty.GetRandomNastyType(GameEngine, true);
            }

            // Reset the nasties
            for (var a = 0; a < _nastyCount; a++)
            {
                _nasties[a].KeepNastyTheSame = GameEngine.GameScreens.CurrentGameScreenData.KeepNastiesTheSame;
                _nasties[a].NastyState = NastyState.PreConception;

                if (GameEngine.GameScreens.CurrentGameScreenData.KeepNastiesTheSame)
                {
                    _nasties[a].KeepNastyTheSame = true;
                    _nasties[a].NastyType = nastyType;
                }
            }
        }

        for (var a = 0; a < _nastyCount; a++)
        {
            _nasties[a].PerformPostChangeScreenActions();
        }

        // Deactivate some nasties if we have conveyer nasties on screen
        if (GameEngine.ConveyerNastiesInstance.NastiesActive > 0)
        {
            for (var a = 0; a < GameEngine.ConveyerNastiesInstance.NastiesActive; a++)
            {
                _nasties[a].NastyState = NastyState.Disabled;
            }
        }

        // Reset the nasty smoke effect
        if (_nastySmokes != null)
        {
            _nastySmokes.Reset();
        }

        _previousScreenVisited = GameEngine.GameScreens.PreviousScreenNumber;
    }

    public override void Draw(GameTime gameTime, int screenFade)
    {
        if (GameEngine.Player.BlobsState == BlobState.Normal)
        {
            for (var a = 0; a < _nastyCount; a++)
            {
                if (_nasties[a].XPos > UnplacedNastyPosition &&
                    _nasties[a].YPos > UnplacedNastyPosition)
                {
                    _nasties[a].Draw(gameTime, screenFade);
                }
            }

            if (_nastySmokes != null)
            {
                _nastySmokes.Draw(screenFade);
            }
        }
    }

    #endregion

}