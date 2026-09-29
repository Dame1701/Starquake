// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.ContentManagement;
using StarquakeXNA.Helpers;
using StarquakeXNA.Particles;
using StarquakeXNA.Structure;

namespace StarquakeXNA;

public class ConveyerNasties : GameComponentBase
{

    #region IGameComponent Members

    public event RaiseInteractionWithBlob OnNastyInteractionWithBlob;

    public List<ConveyerNasty> Nasties { get; private set; }

    public int NastiesActive
    {
        get
        {
            if (gameEngine.GameScreens.GetCoordsOfTiles(391) != null)
            {
                return gameEngine.GameScreens.GetCoordsOfTiles(391).Count;
            }

            return 0;
        }
    }

    public ConveyerNasties(GameEngine GameEngineReference) : base(GameEngineReference)
    {
    }

    public void LoadContent(ContentList Content)
    {
        Nasties = new List<ConveyerNasty>();

        // Create 3 nasties that we will keep re-using
        for (var a = 0; a < 3; a++)
        {
            var newNasty =
                new ConveyerNasty(gameEngine,
                    0,
                    0,
                    0,
                    false);

            newNasty.LoadContent(Content);
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

        var NastiesToGenerate = gameEngine.GameScreens.GetCoordsOfTiles(391);


        if (NastiesToGenerate != null && NastiesToGenerate.Count > 0)
        {
            // We have some..... So reactivate a few or all...
            for (var a = 0; a < NastiesToGenerate.Count; a++)
            {
                Nasties[a].NastyActive = true;
                Nasties[a].XLocation = NastiesToGenerate[a].X;
                Nasties[a].YLocation = NastiesToGenerate[a].Y;
                Nasties[a].MoveSpeed = gameEngine.RandomNumberGenerator.Next(1, 3);

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
            if (Nasties[a].NastyState == enNastyState.Normal)
            {
                if (CollisionDetection.Check(
                        gameEngine.Player.BlobXPos + 8,
                        gameEngine.Player.BlobYPos - 32,
                        gameEngine.Player.BlobWidth - 16,
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
                            OnNastyInteractionWithBlob(enNastyInterationType.HitBlob, enNastyType.Conveyer);
                        }

                        // Trigger the blob animation
                        gameEngine.Player.BlobTouchedByNasty = true;
                    }
                }

                // Has blob shot a nasty
                if (gameEngine.Player.BlobHasFired)
                {
                    if (CollisionDetection.Check(
                            gameEngine.Player.BlobFireX,
                            gameEngine.Player.BlobFireY,
                            32,
                            32,
                            Nasties[a].XLocation,
                            Nasties[a].YLocation,
                            64,
                            64))
                    {
                        if (Nasties[a].NastyActive)
                        {
                            gameEngine.Player.RegisterFireHit();
                            Nasties[a].NastyState = enNastyState.Shot;

                            gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobExplodes");

                            if (OnNastyInteractionWithBlob != null)
                            {
                                OnNastyInteractionWithBlob(enNastyInterationType.BlobShot, enNastyType.Conveyer);
                            }
                        }
                    }
                }
            }
        }
    }

    public override void Draw(GameTime gameTime, int ScreenFade)
    {
        for (var a = 0; a < Nasties.Count; a++)
        {
            if (Nasties[a].NastyActive || Nasties[a].NastyState == enNastyState.Dead)
            {
                Nasties[a].Draw(gameTime, ScreenFade);
            }
        }
    }

    #endregion

}

public class ConveyerNasty : GameComponentBase
{

    private readonly Animation anim;
    private readonly int screenShakeXOffset = 0;
    private readonly int screenShakeYOffset = 0;

    private int deathFadeLevel = 255;
    private float deathScale = 1f;

    public int XLocation { get; set; }

    public enNastyState NastyState { get; set; }

    public int YLocation { get; set; }

    public int MoveSpeed { get; set; }

    public bool NastyActive { get; set; }

    public int AnimationDelay
    {
        get => anim.Delay;
        set => anim.Delay = value;
    }

    #region IGameComponent Members

    private SpriteCollection nastySprite;
    private IStandardParticleEffect conveyerNastyDies;

    private enDirection nastyDirection;

    public ConveyerNasty(
        GameEngine GameEngineReference,
        int Xposition,
        int Yposition,
        int MoveSpeed,
        bool Active) : base(GameEngineReference)
    {
        XLocation = Xposition;
        YLocation = Yposition;
        this.MoveSpeed = MoveSpeed;
        NastyActive = Active;
        anim = new Animation();
        anim.FirstFrame = 0;
        anim.LastFrame = 5;
        anim.Repeat = true;
        anim.Yoyo = false;
        anim.Delay = 2;
        anim.AnimateForwards = true;
        NastyState = enNastyState.Conception;

        deathFadeLevel = 255;
        deathScale = 1f;
    }

    public void LoadContent(ContentList Content)
    {
        nastySprite = new SpriteCollection(gameEngine, gameEngine.ContentMgr, Content.GetContent("conveyerNasty"), 64, 64);
        conveyerNastyDies = gameEngine.ContentResourceManager.GetParticleEffectInstance("NastyDies", enParticleEffectType.Triggered, null) as IStandardParticleEffect;
        if (conveyerNastyDies != null)
        {
            conveyerNastyDies.ParticleEffectComplete += conveyerNastyDies_ParticleEffectComplete;
        }
    }

    private void conveyerNastyDies_ParticleEffectComplete()
    {
        NastyActive = false;
    }

    public void Initialise()
    {
        NastyState = enNastyState.Conception;
        NastyActive = true;

        var randomDirection = gameEngine.RandomNumberGenerator.Next(0, 2);

        if (randomDirection == 0)
        {
            nastyDirection = enDirection.Left;
        }
        else
        {
            nastyDirection = enDirection.Right;
        }
    }

    public void PerformPostChangeScreenActions()
    {
        if (conveyerNastyDies != null)
        {
            conveyerNastyDies.Stop();
        }

        NastyActive = false;
    }

    public override void Update(GameTime gameTime)
    {
        if (conveyerNastyDies != null)
        {
            // Update the explosion
            conveyerNastyDies.Update(XLocation + screenShakeXOffset + 32, YLocation + screenShakeYOffset + 64, gameTime);
        }

        if (NastyState == enNastyState.Conception)
        {
            deathFadeLevel = 255;
            deathScale = 1f;
            NastyState = enNastyState.Normal;
        }
        else if (NastyState == enNastyState.Normal)
        {
            if (nastyDirection == enDirection.Left)
            {
                var amountToMove = gameEngine.GameScreens.CheckCollision(enDirection.Left, XLocation, YLocation + 32, 64, 64, MoveSpeed, false);
                if (amountToMove > 0)
                {
                    XLocation = XLocation - amountToMove;
                }
                else
                {
                    nastyDirection = enDirection.Right;
                    anim.AnimateForwards = true;
                }
            }
            else if (nastyDirection == enDirection.Right)
            {
                var amountToMove = gameEngine.GameScreens.CheckCollision(enDirection.Right, XLocation, YLocation + 32, 64, 64, MoveSpeed, false);
                if (amountToMove > 0)
                {
                    XLocation = XLocation + amountToMove;
                }
                else
                {
                    nastyDirection = enDirection.Left;
                    anim.AnimateForwards = false;
                }
            }
            anim.GetNextFrame();
        }
        else if (NastyState == enNastyState.Shot)
        {
            deathFadeLevel = 255;
            deathScale = 1.0f;

            if (conveyerNastyDies != null)
            {
                conveyerNastyDies.Start();
            }

            NastyState = enNastyState.Dead;
            NastyActive = false;
        }
    }

    public override void Draw(GameTime gameTime, int ScreenFade)
    {
        if (NastyState == enNastyState.Normal)
        {
            nastySprite.Begin(BlendState.AlphaBlend, gameTime);
            nastySprite.DrawSprite(anim.CurrentFrame, XLocation, YLocation + 32, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)ScreenFade));
            nastySprite.End();
        }
        else if (NastyState == enNastyState.Dead)
        {
            // Draw the explosion
            if (conveyerNastyDies != null)
            {
                conveyerNastyDies.Draw();
            }

            // Fade the nasty out as it explodes
            if (deathFadeLevel > 0)
            {
                deathFadeLevel = deathFadeLevel - 20;
            }

            if (deathFadeLevel < 0)
            {
                deathFadeLevel = 0;
            }

            // Shrink the nasty as it explodes
            if (deathScale > 0)
            {
                deathScale = deathScale - 0.01f;
            }

            if (deathScale < 0)
            {
                deathScale = 0;
            }

            if (deathFadeLevel > 0)
            {
                nastySprite.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                nastySprite.DrawSprite(anim.CurrentFrame, XLocation, YLocation + 32, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)(deathFadeLevel * ((double)ScreenFade / 255))), deathScale, 0f);
                nastySprite.End();
            }
        }
    }

    #endregion

}