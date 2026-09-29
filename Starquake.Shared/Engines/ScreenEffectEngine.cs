// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Structure;

namespace StarquakeXNA.Engines;

public class RenderViewPort
{
    private readonly GameEngine _gameEngine;

    public RenderViewPort(GameEngine gameEngineReference, string name, int xPos, int yPos, int width, int height)
    {
        _gameEngine = gameEngineReference;

        MasterXPos = xPos;
        MasterYPos = yPos;

        this.Width = width;
        this.Height = height;

        RenderTarget = new RenderTarget2D(_gameEngine.GraphicsDeviceMgr.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);


        this.XPos = xPos;
        this.YPos = yPos;
        this.Name = name;
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

    private readonly SpriteBatch _renderer;
    private readonly List<RenderViewPort> _renderViewPorts;
    private readonly List<ScreenEffectBase> _screenEffects;

    private RenderViewPort _activeViewPort;


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
        _screenEffects.Add(new AddictiveFade(GameEngine));

        _renderer = new SpriteBatch(GameEngine.GraphicsDeviceMgr.GraphicsDevice);
        HaltBlobAndNasties = false;
    }
    public bool HaltBlobAndNasties { get; private set; }

    public ScreenEffectBase ActiveScreenEffect { get; private set; }

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
        if (GetRenderViewPort(name) == null)
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

        if (selectedViewPort != null)
        {
            selectedViewPort.Active = true;
            _activeViewPort = selectedViewPort;
            GameEngine.GraphicsDeviceMgr.GraphicsDevice.SetRenderTarget(selectedViewPort.RenderTarget);

            if (clearScreen)
            {
                GameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(Color.Transparent);
            }
        }
        else
        {
            throw new Exception("Could not find a RenderViewPort with the same name '" + name + "'");
        }
    }

    public void MoveViewPort(string name, int xOffset, int yOffset)
    {
        var viewPort = GetRenderViewPort(name);
        viewPort.XPos = viewPort.MasterXPos + xOffset;
        viewPort.YPos = viewPort.MasterYPos + yOffset;
    }

    public RenderViewPort GetRenderViewPort(string name)
    {
        for (var a = 0; a < _renderViewPorts.Count; a++)
        {
            if (_renderViewPorts[a].Name.ToLower() == name.ToLower())
            {
                return _renderViewPorts[a];
            }
        }

        return null;
    }

    public void ExecuteScreenEffect(ScreenEffectType screenEffectToExecute, int effectSpeed, int effectDuration, bool haltBlobAndNasties)
    {
        // Only allow one effect to execute at a time - may need to implement an overrride system....
        for (var a = 0; a < _screenEffects.Count; a++)
        {
            _screenEffects[a].Reset();
            ActiveScreenEffect = null;
        }

        for (var a = 0; a < _screenEffects.Count; a++)
        {
            if (_screenEffects[a].ScreenEffectType == screenEffectToExecute)
            {
                ActiveScreenEffect = _screenEffects[a];
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

        for (var a = 0; a < _renderViewPorts.Count; a++)
        {
            if (_renderViewPorts[a].Active)
            {
                // Only apply effects to the game area - might change this so we can apply different ones to different ports...
                if (_renderViewPorts[a].Name.ToLower() == "gamearea")
                {
                    // Run the alternative renderer if we need to
                    var useDefaultRenderer = true;

                    if (ActiveScreenEffect != null)
                    {
                        useDefaultRenderer = ActiveScreenEffect.Draw(_renderer, _renderViewPorts[a]);
                    }

                    // We're using the default renderer so lets render
                    if (useDefaultRenderer)
                    {
                        _renderer.Begin(SpriteSortMode.Deferred, BlendState.Opaque, null, null, null, null,
                            GameEngine.PresentationTransform);

                        _renderer.Draw(_renderViewPorts[a].RenderTarget, new Rectangle(
                            _renderViewPorts[a].XPos,
                            _renderViewPorts[a].YPos,
                            _renderViewPorts[a].Width,
                            _renderViewPorts[a].Height), Color.White);

                        _renderer.End();
                    }
                }
                else
                {
                    _renderer.Begin(SpriteSortMode.Deferred, BlendState.Opaque, null, null, null, null,
                        GameEngine.PresentationTransform);

                    _renderer.Draw(_renderViewPorts[a].RenderTarget, new Rectangle(
                        _renderViewPorts[a].XPos,
                        _renderViewPorts[a].YPos,
                        _renderViewPorts[a].Width,
                        _renderViewPorts[a].Height), Color.White);

                    _renderer.End();
                }

                _renderViewPorts[a].Active = false;
            }
        }

    }
}