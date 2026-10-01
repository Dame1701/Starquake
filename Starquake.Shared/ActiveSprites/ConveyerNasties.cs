// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Starquake.Collisions;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

public class ConveyerNasties : GameComponentBase
{
    private readonly Blob _blob;
    private readonly ContentManager _contentManager;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsDeviceManager _graphicsDeviceManager;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;

    #region IGameComponent Members

    public event RaiseInteractionWithBlob? OnNastyInteractionWithBlob;

    public List<ConveyerNasty> Nasties { get; }

    public int NastiesActive
    {
        get
        {
            if (_gameScreens.GetCoordsOfTiles(391) != null)
            {
                return _gameScreens.GetCoordsOfTiles(391).Count;
            }

            return 0;
        }
    }

    public ConveyerNasties(
        ResourceManager resources,
        ContentManager content,
        GameScreenCollection gameScreens,
        GraphicsDeviceManager graphics,
        Blob blob,
        Random randomNumberGenerator)
    {
        _resources = resources;
        _contentManager = content;
        _gameScreens = gameScreens;
        _graphicsDeviceManager = graphics;
        _blob = blob;
        _randomNumberGenerator = randomNumberGenerator;

        Nasties = new List<ConveyerNasty>();
    }

    public void LoadContent(ContentList content)
    {
        // Create 3 nasties that we will keep re-using
        Nasties.Clear();
        for (int a = 0; a < 3; a++)
        {
            Nasties.Add(
                new ConveyerNasty(_resources,
                    _contentManager,
                    _gameScreens,
                    _graphicsDeviceManager,
                    _randomNumberGenerator,
                    0,
                    0,
                    0,
                    false,
                    content));
        }
    }

    public void PerformPostChangeScreenActions()
    {
        // See if we need to create some nasties and get on with it

        // Deactivate all nasties first
        foreach (ConveyerNasty nasty in Nasties)
        {
            nasty.PerformPostChangeScreenActions();
        }

        List<Point>? nastiesToGenerate = _gameScreens.GetCoordsOfTiles(391);


        if (nastiesToGenerate != null && nastiesToGenerate.Count > 0)
        {
            // We have some..... So reactivate a few or all...
            for (int a = 0; a < nastiesToGenerate.Count; a++)
            {
                Nasties[a].NastyActive = true;
                Nasties[a].XLocation = nastiesToGenerate[a].X;
                Nasties[a].YLocation = nastiesToGenerate[a].Y;
                Nasties[a].MoveSpeed = _randomNumberGenerator.Next(1, 3);

                if (Nasties[a].MoveSpeed == 1)
                {
                    Nasties[a].AnimationDelay = 6;
                }
                else if (Nasties[a].MoveSpeed == 2)
                {
                    Nasties[a].AnimationDelay = 4;
                }
                else if (Nasties[a].MoveSpeed == 3)
                {
                    Nasties[a].AnimationDelay = 2;
                }

                Nasties[a].Initialise(null);
            }
        }
    }

    public override void Update(GameTime gameTime)
    {
        for (int a = 0; a < Nasties.Count; a++)
        {
            Nasties[a].Update(gameTime);

            // Check for a collision between a nasty and Blob and raise an event
            // Since we have a reference to Blob directly, we can send the call to animate contact directly
            // We raise the event to affect the status panel
            // Going to move the offsets etc. in a bit so it's not too precise and unfair!
            if (Nasties[a].NastyState == NastyState.Normal)
            {
                if (CollisionDetection.Check(
                        _blob.BlobXPos + 8,
                        _blob.BlobYPos - 32,
                        _blob.BlobWidth - 16,
                        64,
                        Nasties[a].XLocation,
                        Nasties[a].YLocation,
                        64,
                        64))
                {
                    // Raise the event
                    if (Nasties[a].NastyActive)
                    {
                        if (OnNastyInteractionWithBlob != null)
                        {
                            OnNastyInteractionWithBlob(NastyInteractionType.HitBlob, NastyType.Conveyer);
                        }

                        // Trigger the blob animation
                        _blob.BlobTouchedByNasty = true;
                    }
                }

                // Has blob shot a nasty
                if (_blob.BlobHasFired)
                {
                    if (CollisionDetection.Check(
                            _blob.BlobFireX,
                            _blob.BlobFireY,
                            32,
                            32,
                            Nasties[a].XLocation,
                            Nasties[a].YLocation,
                            64,
                            64))
                    {
                        if (Nasties[a].NastyActive)
                        {
                            _blob.RegisterFireHit();
                            Nasties[a].NastyState = NastyState.Shot;

                            _resources.GetSoundFxEngine().PlaySound("BlobExplodes");

                            if (OnNastyInteractionWithBlob != null)
                            {
                                OnNastyInteractionWithBlob(NastyInteractionType.BlobShot, NastyType.Conveyer);
                            }
                        }
                    }
                }
            }
        }
    }

    public override void Draw(GameTime gameTime, int screenFade)
    {
        foreach (ConveyerNasty nasty in Nasties)
        {
            if (nasty.NastyActive || nasty.NastyState == NastyState.Dead)
            {
                nasty.Draw(gameTime, screenFade);
            }
        }
    }

    #endregion

}
