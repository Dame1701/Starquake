// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Constants;
using Starquake.Display;
using Starquake.Structure;

namespace Starquake.Engines.ScreenEffects;

internal class ScreenEffectEngine
{
    private readonly GraphicsDeviceManager _graphicsDeviceManager;
    private readonly PresentationSettings _presentation;

    private readonly SpriteBatch _renderer;
    private readonly List<RenderViewPort> _renderViewPorts;
    private readonly List<ScreenEffectBase> _screenEffects;




    public ScreenEffectEngine(
        GraphicsDeviceManager graphics,
        PresentationSettings presentation)
    {
        _graphicsDeviceManager = graphics;
        _presentation = presentation;


        _renderViewPorts = new List<RenderViewPort>();
        _screenEffects = new List<ScreenEffectBase>();

        // Add all of the screen effects we can find - do this eventually through reflection but for now - manually!
        _screenEffects.Add(new LeftTunnelEntryEffect());
        _screenEffects.Add(new RightTunnelEntryEffect());
        _screenEffects.Add(new AdditiveFade(presentation));

        _renderer = new SpriteBatch(_graphicsDeviceManager.GraphicsDevice);
        _haltBlobAndNastiesRequested = false;
    }
    private bool _haltBlobAndNastiesRequested;

    /// <summary>
    ///     Whether the blob and the nasties are being held still. Both that the caller asked for a
    ///     halt and that the running effect still wants one - the tunnel transition lets go before
    ///     it has finished, so the last of it settles while the blob is already walking.
    /// </summary>
    public bool HaltBlobAndNasties
        => _haltBlobAndNastiesRequested && ActiveScreenEffect is { HoldsGameplay: true };

    public ScreenEffectBase? ActiveScreenEffect { get; private set; }

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
            _renderViewPorts.Add(new RenderViewPort(_graphicsDeviceManager, name, xPosition, yPosition, width, height));
        }
        else
        {
            throw new Exception("A RenderViewPort with the same name already exists");
        }
    }

    public void SelectRenderViewPort(string name, bool clearScreen = true)
    {
        RenderViewPort selectedViewPort = GetRenderViewPort(name);

        selectedViewPort.Active = true;
        _graphicsDeviceManager.GraphicsDevice.SetRenderTarget(selectedViewPort.RenderTarget);

        if (clearScreen)
        {
            _graphicsDeviceManager.GraphicsDevice.Clear(Color.Transparent);
        }
    }

    public void MoveViewPort(string name, int xOffset, int yOffset)
    {
        RenderViewPort viewPort = GetRenderViewPort(name);
        viewPort.XPos = viewPort.MasterXPos + xOffset;
        viewPort.YPos = viewPort.MasterYPos + yOffset;
    }

    private RenderViewPort? FindRenderViewPort(string name)
    {
        foreach (RenderViewPort renderViewPort in _renderViewPorts)
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
        foreach (ScreenEffectBase screenEffect in _screenEffects)
        {
            screenEffect.Reset();
            ActiveScreenEffect = null;
        }

        foreach (ScreenEffectBase screenEffect in _screenEffects)
        {
            if (screenEffect.ScreenEffectType == screenEffectToExecute)
            {
                ActiveScreenEffect = screenEffect;
                ActiveScreenEffect.TriggerEffect(GetRenderViewPort(ViewPorts.GameArea), effectSpeed, effectDuration);
                _haltBlobAndNastiesRequested = haltBlobAndNasties;
                break;
            }
        }
    }

    public void Update()
    {
        if (ActiveScreenEffect != null)
        {
            ActiveScreenEffect.Update();

            if (ActiveScreenEffect.EffectFinished)
            {
                ActiveScreenEffect = null;
            }
        }
    }

    public void Draw()
    {
        // Nothing to compose until the view ports have been registered, and clearing the back
        // buffer to draw nothing over it would wipe out whatever did draw - the boot screen, which
        // is on screen for the whole of the content load and is drawn straight to the back buffer.
        // This used to be guaranteed by the engine not existing yet; it is built before the view
        // ports now, so it has to say so itself.
        if (_renderViewPorts.Count == 0)
        {
            return;
        }

        // Select the back buffer
        _graphicsDeviceManager.GraphicsDevice.SetRenderTarget(null);

        // Clear the screen
        _graphicsDeviceManager.GraphicsDevice.Clear(_presentation.ScreenClearColour);

        foreach (RenderViewPort renderViewPort in _renderViewPorts)
        {
            if (renderViewPort.Active)
            {
                // Only apply effects to the game area - might change this so we can apply different ones to different ports...
                if (renderViewPort.Name.ToLower() == "gamearea")
                {
                    // Run the alternative renderer if we need to
                    bool useDefaultRenderer = true;

                    if (ActiveScreenEffect != null)
                    {
                        useDefaultRenderer = ActiveScreenEffect.Draw(_renderer, renderViewPort);
                    }

                    // We're using the default renderer so lets render
                    if (useDefaultRenderer)
                    {
                        _renderer.Begin(SpriteSortMode.Deferred, BlendState.Opaque, null, null, null, null,
                            _presentation.Transform);

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
                        _presentation.Transform);

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
