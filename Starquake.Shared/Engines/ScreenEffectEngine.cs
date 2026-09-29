// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Structure;

namespace StarquakeXNA;

public class RenderViewPort
{
    private readonly GameEngine gameEngine;

    public RenderViewPort(GameEngine GameEngineReference, string Name, int XPos, int YPos, int Width, int Height)
    {
        gameEngine = GameEngineReference;

        MasterXPos = XPos;
        MasterYPos = YPos;

        this.Width = Width;
        this.Height = Height;

        RenderTarget = new RenderTarget2D(gameEngine.GraphicsDeviceMgr.GraphicsDevice, Width, Height, false, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);


        this.XPos = XPos;
        this.YPos = YPos;
        this.Name = Name;
        Active = false;
    }
    public RenderTarget2D RenderTarget { get; }

    public bool Active { get; set; }

    public int XPos { get; set; }

    public int YPos { get; set; }

    public int MasterXPos { get; }

    public int MasterYPos { get; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int MasterWidth => RenderTarget.Width;

    public int MasterHeight => RenderTarget.Height;
    public string Name { get; }

    public Rectangle GetBounds()
        => new(XPos, YPos, Width, Height);
}

public class ScreenEffectEngine : GameComponentBase
{

    private readonly SpriteBatch renderer;
    private readonly List<RenderViewPort> renderViewPorts;
    private readonly List<ScreenEffectBase> screenEffects;

    private RenderViewPort activeViewPort;


    private int screenHeight;

    private int screenWidth;

    public ScreenEffectEngine(
        int ScreenWidth,
        int ScreenHeight,
        GameEngine GameEngineReference)
        : base(GameEngineReference)
    {
        screenWidth = ScreenWidth;
        screenHeight = ScreenHeight;

        renderViewPorts = new List<RenderViewPort>();
        screenEffects = new List<ScreenEffectBase>();

        // Add all of the screen effects we can find - do this eventually through reflection but for now - manually!
        screenEffects.Add(new LeftTunnelEntryEffect(gameEngine));
        screenEffects.Add(new RightTunnelEntryEffect(gameEngine));
        screenEffects.Add(new AddictiveFade(gameEngine));

        renderer = new SpriteBatch(gameEngine.GraphicsDeviceMgr.GraphicsDevice);
        HaltBlobAndNasties = false;
    }
    public bool HaltBlobAndNasties { get; private set; }

    public ScreenEffectBase ActiveScreenEffect { get; private set; }

    public enScreenEffectType CurrentScreenEffect
    {
        get
        {
            if (ActiveScreenEffect != null)
            {
                return ActiveScreenEffect.ScreenEffectType;
            }

            return enScreenEffectType.None;
        }
    }

    /// <summary>
    ///     Rather than adding a viewport this sets up a new Render2D target at the size of the viewport we want, we also store
    ///     the location so we can draw correctly
    /// </summary>
    /// <param name="?"></param>
    public void RegisterViewPort(string Name,
        int XPosition,
        int YPosition,
        int Width,
        int Height)
    {
        if (GetRenderViewPort(Name) == null)
        {
            renderViewPorts.Add(new RenderViewPort(gameEngine, Name, XPosition, YPosition, Width, Height));
        }
        else
        {
            throw new Exception("A RenderViewPort with the same name already exists");
        }
    }

    public void SelectRenderViewPort(string Name, bool ClearScreen = true)
    {
        var selectedViewPort = GetRenderViewPort(Name);

        if (selectedViewPort != null)
        {
            selectedViewPort.Active = true;
            activeViewPort = selectedViewPort;
            gameEngine.GraphicsDeviceMgr.GraphicsDevice.SetRenderTarget(selectedViewPort.RenderTarget);

            if (ClearScreen)
            {
                gameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(Color.Transparent);
            }
        }
        else
        {
            throw new Exception("Could not find a RenderViewPort with the same name '" + Name + "'");
        }
    }

    public void MoveViewPort(string Name, int XOffset, int YOffset)
    {
        var viewPort = GetRenderViewPort(Name);
        viewPort.XPos = viewPort.MasterXPos + XOffset;
        viewPort.YPos = viewPort.MasterYPos + YOffset;
    }

    public RenderViewPort GetRenderViewPort(string Name)
    {
        for (var a = 0; a < renderViewPorts.Count; a++)
        {
            if (renderViewPorts[a].Name.ToLower() == Name.ToLower())
            {
                return renderViewPorts[a];
            }
        }

        return null;
    }

    public void ExecuteScreenEffect(enScreenEffectType ScreenEffectToExecute, int EffectSpeed, int EffectDuration, bool HaltBlobAndNasties)
    {
        // Only allow one effect to execute at a time - may need to implement an overrride system....
        for (var a = 0; a < screenEffects.Count; a++)
        {
            screenEffects[a].Reset();
            ActiveScreenEffect = null;
        }

        for (var a = 0; a < screenEffects.Count; a++)
        {
            if (screenEffects[a].ScreenEffectType == ScreenEffectToExecute)
            {
                ActiveScreenEffect = screenEffects[a];
                ActiveScreenEffect.TriggerEffect(GetRenderViewPort("GameArea"), EffectSpeed, EffectDuration);
                this.HaltBlobAndNasties = HaltBlobAndNasties;
                break;
            }
        }
    }

    public int GetParticleX(int XPos)
        => XPos;

    public int GetParticleY(int YPos)
        => YPos;

    public void Update()
    {
        if (ActiveScreenEffect != null)
        {
            ActiveScreenEffect.Update();

            if (ActiveScreenEffect.EffectFinished)
            {
                HaltBlobAndNasties = false;
                ActiveScreenEffect = null;
            }
        }
    }

    public void Draw()
    {
        // Select the back buffer
        gameEngine.GraphicsDeviceMgr.GraphicsDevice.SetRenderTarget(null);

        // Clear the screen
        gameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(gameEngine.ScreenClearColour);

        for (var a = 0; a < renderViewPorts.Count; a++)
        {
            if (renderViewPorts[a].Active)
            {
                // Only apply effects to the game area - might change this so we can apply different ones to different ports...
                if (renderViewPorts[a].Name.ToLower() == "gamearea")
                {
                    // Run the alternative renderer if we need to
                    var useDefaultRenderer = true;

                    if (ActiveScreenEffect != null)
                    {
                        useDefaultRenderer = ActiveScreenEffect.Draw(renderer, renderViewPorts[a]);
                    }

                    // We're using the default renderer so lets render
                    if (useDefaultRenderer)
                    {
                        renderer.Begin(SpriteSortMode.Deferred, BlendState.Opaque, null, null, null, null,
                            gameEngine.PresentationTransform);

                        renderer.Draw(renderViewPorts[a].RenderTarget, new Rectangle(
                            renderViewPorts[a].XPos,
                            renderViewPorts[a].YPos,
                            renderViewPorts[a].Width,
                            renderViewPorts[a].Height), Color.White);

                        renderer.End();
                    }
                }
                else
                {
                    renderer.Begin(SpriteSortMode.Deferred, BlendState.Opaque, null, null, null, null,
                        gameEngine.PresentationTransform);

                    renderer.Draw(renderViewPorts[a].RenderTarget, new Rectangle(
                        renderViewPorts[a].XPos,
                        renderViewPorts[a].YPos,
                        renderViewPorts[a].Width,
                        renderViewPorts[a].Height), Color.White);

                    renderer.End();
                }

                renderViewPorts[a].Active = false;
            }
        }

    }
}