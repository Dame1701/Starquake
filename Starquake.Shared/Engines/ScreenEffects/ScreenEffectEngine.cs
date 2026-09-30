// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Structure;

namespace Starquake.Engines.ScreenEffects;

public class ScreenEffectEngine : GameComponentBase
{

    private readonly SpriteBatch _renderer;
    private readonly List<RenderViewPort> _renderViewPorts;
    private readonly List<ScreenEffectBase> _screenEffects;


    private int _screenHeight;

    private int _screenWidth;

    public ScreenEffectEngine(
        int screenWidth,
        int screenHeight,
        GameEngine gameEngineReference)
        : base(gameEngineReference)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        _renderViewPorts = new List<RenderViewPort>();
        _screenEffects = new List<ScreenEffectBase>();

        // Add all of the screen effects we can find - do this eventually through reflection but for now - manually!
        _screenEffects.Add(new LeftTunnelEntryEffect(GameEngine));
        _screenEffects.Add(new RightTunnelEntryEffect(GameEngine));
        _screenEffects.Add(new AdditiveFade(GameEngine));

        _renderer = new SpriteBatch(GameEngine.GraphicsDeviceMgr.GraphicsDevice);
        HaltBlobAndNasties = false;
    }
    public bool HaltBlobAndNasties { get; private set; }

    public ScreenEffectBase? ActiveScreenEffect { get; private set; }

    public ScreenEffectType CurrentScreenEffect
    {
        get
        {
            if (ActiveScreenEffect != null)
            {
                return ActiveScreenEffect.ScreenEffectType;
            }

            return ScreenEffectType.None;
        }
    }

    /// <summary>
    ///     Rather than adding a viewport this sets up a new Render2D target at the size of the viewport we want, we also store
    ///     the location so we can draw correctly
    /// </summary>
    /// <param name="?"></param>
    public void RegisterViewPort(string name,
        int xPosition,
        int yPosition,
        int width,
        int height)
    {
        if (FindRenderViewPort(name) == null)
        {
            _renderViewPorts.Add(new RenderViewPort(GameEngine, name, xPosition, yPosition, width, height));
        }
        else
        {
            throw new Exception("A RenderViewPort with the same name already exists");
        }
    }

    public void SelectRenderViewPort(string name, bool clearScreen = true)
    {
        var selectedViewPort = GetRenderViewPort(name);

        selectedViewPort.Active = true;
        GameEngine.GraphicsDeviceMgr.GraphicsDevice.SetRenderTarget(selectedViewPort.RenderTarget);

        if (clearScreen)
        {
            GameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(Color.Transparent);
        }
    }

    public void MoveViewPort(string name, int xOffset, int yOffset)
    {
        var viewPort = GetRenderViewPort(name);
        viewPort.XPos = viewPort.MasterXPos + xOffset;
        viewPort.YPos = viewPort.MasterYPos + yOffset;
    }

    private RenderViewPort? FindRenderViewPort(string name)
    {
        foreach (var renderViewPort in _renderViewPorts)
        {
            if (renderViewPort.Name.ToLower() == name.ToLower())
            {
                return renderViewPort;
            }
        }

        return null;
    }

    private RenderViewPort GetRenderViewPort(string name)
        => FindRenderViewPort(name)
           ?? throw new InvalidOperationException($"Could not find a RenderViewPort named '{name}'.");

    public void ExecuteScreenEffect(ScreenEffectType screenEffectToExecute, int effectSpeed, int effectDuration, bool haltBlobAndNasties)
    {
        // Only allow one effect to execute at a time - may need to implement an overrride system....
        foreach (var screenEffect in _screenEffects)
        {
            screenEffect.Reset();
            ActiveScreenEffect = null;
        }

        foreach (var screenEffect in _screenEffects)
        {
            if (screenEffect.ScreenEffectType == screenEffectToExecute)
            {
                ActiveScreenEffect = screenEffect;
                ActiveScreenEffect.TriggerEffect(GetRenderViewPort("GameArea"), effectSpeed, effectDuration);
                this.HaltBlobAndNasties = haltBlobAndNasties;
                break;
            }
        }
    }

    public int GetParticleX(int xPos)
        => xPos;

    public int GetParticleY(int yPos)
        => yPos;

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
        GameEngine.GraphicsDeviceMgr.GraphicsDevice.SetRenderTarget(null);

        // Clear the screen
        GameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(GameEngine.ScreenClearColour);

        foreach (var renderViewPort in _renderViewPorts)
        {
            if (renderViewPort.Active)
            {
                // Only apply effects to the game area - might change this so we can apply different ones to different ports...
                if (renderViewPort.Name.ToLower() == "gamearea")
                {
                    // Run the alternative renderer if we need to
                    var useDefaultRenderer = true;

                    if (ActiveScreenEffect != null)
                    {
                        useDefaultRenderer = ActiveScreenEffect.Draw(_renderer, renderViewPort);
                    }

                    // We're using the default renderer so lets render
                    if (useDefaultRenderer)
                    {
                        _renderer.Begin(SpriteSortMode.Deferred, BlendState.Opaque, null, null, null, null,
                            GameEngine.PresentationTransform);

                        _renderer.Draw(renderViewPort.RenderTarget, new Rectangle(
                            renderViewPort.XPos,
                            renderViewPort.YPos,
                            renderViewPort.Width,
                            renderViewPort.Height), Color.White);

                        _renderer.End();
                    }
                }
                else
                {
                    _renderer.Begin(SpriteSortMode.Deferred, BlendState.Opaque, null, null, null, null,
                        GameEngine.PresentationTransform);

                    _renderer.Draw(renderViewPort.RenderTarget, new Rectangle(
                        renderViewPort.XPos,
                        renderViewPort.YPos,
                        renderViewPort.Width,
                        renderViewPort.Height), Color.White);

                    _renderer.End();
                }

                renderViewPort.Active = false;
            }
        }

    }
}