// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Starquake.Collisions;
using Starquake.ContentManagement;
using Starquake.Engines;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

public class ConveyerNasties : GameComponentBase
{

    #region IGameComponent Members

    public event RaiseInteractionWithBlob OnNastyInteractionWithBlob;

    public List<ConveyerNasty> Nasties { get; private set; }

    public int NastiesActive
    {
        get
        {
            if (GameEngine.GameScreens.GetCoordsOfTiles(391) != null)
            {
                return GameEngine.GameScreens.GetCoordsOfTiles(391).Count;
            }

            return 0;
        }
    }

    public ConveyerNasties(GameEngine gameEngineReference) : base(gameEngineReference)
    {
    }

    public void LoadContent(ContentList content)
    {
        Nasties = new List<ConveyerNasty>();

        // Create 3 nasties that we will keep re-using
        for (var a = 0; a < 3; a++)
        {
            var newNasty =
                new ConveyerNasty(GameEngine,
                    0,
                    0,
                    0,
                    false);

            newNasty.LoadContent(content);
            Nasties.Add(newNasty);

        }
    }

    public void PerformPostChangeScreenActions()
    {
        // See if we need to create some nasties and get on with it

        // Deactivate all nasties first
        for (var a = 0; a < Nasties.Count; a++)
        {
            Nasties[a].PerformPostChangeScreenActions();
        }

        var nastiesToGenerate = GameEngine.GameScreens.GetCoordsOfTiles(391);


        if (nastiesToGenerate != null && nastiesToGenerate.Count > 0)
        {
            // We have some..... So reactivate a few or all...
            for (var a = 0; a < nastiesToGenerate.Count; a++)
            {
                Nasties[a].NastyActive = true;
                Nasties[a].XLocation = nastiesToGenerate[a].X;
                Nasties[a].YLocation = nastiesToGenerate[a].Y;
                Nasties[a].MoveSpeed = GameEngine.RandomNumberGenerator.Next(1, 3);

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
        for (var a = 0; a < Nasties.Count; a++)
        {
            Nasties[a].Update(gameTime);

            // Check for a collision between a nasty and Blob and raise an event
            // Since we have a reference to Blob directly, we can send the call to animate contact directly
            // We raise the event to affect the status panel
            // Going to move the offsets etc. in a bit so it's not too precise and unfair!
            if (Nasties[a].NastyState == NastyState.Normal)
            {
                if (CollisionDetection.Check(
                        GameEngine.Player.BlobXPos + 8,
                        GameEngine.Player.BlobYPos - 32,
                        GameEngine.Player.BlobWidth - 16,
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
                            OnNastyInteractionWithBlob(NastyInterationType.HitBlob, NastyType.Conveyer);
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
                            32,
                            32,
                            Nasties[a].XLocation,
                            Nasties[a].YLocation,
                            64,
                            64))
                    {
                        if (Nasties[a].NastyActive)
                        {
                            GameEngine.Player.RegisterFireHit();
                            Nasties[a].NastyState = NastyState.Shot;

                            GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobExplodes");

                            if (OnNastyInteractionWithBlob != null)
                            {
                                OnNastyInteractionWithBlob(NastyInterationType.BlobShot, NastyType.Conveyer);
                            }
                        }
                    }
                }
            }
        }
    }

    public override void Draw(GameTime gameTime, int screenFade)
    {
        for (var a = 0; a < Nasties.Count; a++)
        {
            if (Nasties[a].NastyActive || Nasties[a].NastyState == NastyState.Dead)
            {
                Nasties[a].Draw(gameTime, screenFade);
            }
        }
    }

    #endregion

}