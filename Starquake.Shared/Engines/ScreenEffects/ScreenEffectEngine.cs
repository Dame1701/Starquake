// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;

namespace Starquake.Engines.ScreenEffects;

internal class ScreenEffectEngine
{
    private readonly GraphicsDeviceManager _graphicsDeviceManager;
    private readonly PresentationSettings _presentation;

    private readonly SpriteBatch _renderer;
    private readonly List<RenderViewPort> _renderViewPorts;
    private readonly List<ScreenEffectBase> _screenEffects;
    private bool _haltBlobAndNastiesRequested;


    public ScreenEffectEngine(
        GraphicsDeviceManager graphics,
        PresentationSettings presentation,
        ResourceManager resources)
    {
        _graphicsDeviceManager = graphics;
        _presentation = presentation;


        _renderViewPorts = new List<RenderViewPort>();
        _screenEffects = new List<ScreenEffectBase>();

        // One snapshot between the two tunnel directions: only one effect is ever active, so they
        // cannot both be mid-transition and want a picture of their own.
        var outgoingScreen = new ScreenSnapshot(graphics);

        _screenEffects.Add(new LeftTunnelEntryEffect(outgoingScreen, presentation, resources));
        _screenEffects.Add(new RightTunnelEntryEffect(outgoingScreen, presentation, resources));
        _screenEffects.Add(new AdditiveFade(presentation));
        _screenEffects.Add(new TeleportArrivalEffect(presentation, resources));
        _screenEffects.Add(new CoreSurgeEffect(presentation, resources));
        _screenEffects.Add(new WaterEffect(presentation, resources));

        _renderer = new SpriteBatch(_graphicsDeviceManager.GraphicsDevice);
        _haltBlobAndNastiesRequested = false;
    }

    /// <summary>
    ///     Whether Blob and the nasties are being held still. Both that the caller asked for a
    ///     halt and that the running effect still wants one - the tunnel transition lets go before
    ///     it has finished, so the last of it settles while Blob is already walking.
    /// </summary>
    public bool HaltBlobAndNasties
        => _haltBlobAndNastiesRequested && ActiveScreenEffect is { HoldsGameplay: true };

    public ScreenEffectBase? ActiveScreenEffect { get; private set; }

    /// <summary>
    ///     Sets up a named <c>RenderTarget2D</c> at the size wanted, rather than a viewport on the
    ///     back buffer, and remembers where on the screen it is to be drawn.
    /// </summary>
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

    /// <summary>
    ///     Says that one view port should be composed through a pixel shader this frame, rather
    ///     than handed to the back buffer as it is. The full-screen pages' way in: they are not the
    ///     play area, so <see cref="ExecuteScreenEffect" /> cannot reach them, and what the
    ///     computer report's malfunction has to distort is the finished page - text, artwork and
    ///     all - which only exists once the page has drawn itself into its target.
    /// </summary>
    public void ComposeViewPortThrough(string name, Effect shader)
        => GetRenderViewPort(name).ComposeThrough = shader;

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

    /// <summary>
    ///     Says where the next core surge should be centred. Separate from triggering it because
    ///     <see cref="ExecuteScreenEffect" /> is the same call for every effect and only this one
    ///     has somewhere to be.
    /// </summary>
    public void WringCoreSurgeAbout(Vector2 slotInPlayfield)
    {
        foreach (ScreenEffectBase screenEffect in _screenEffects)
        {
            if (screenEffect is CoreSurgeEffect surge)
            {
                surge.WringAbout(slotInPlayfield);

                return;
            }
        }
    }

    public void ExecuteScreenEffect(ScreenEffectType screenEffectToExecute, int effectSpeed, int effectDuration, bool haltBlobAndNasties)
    {
        // Only one effect runs at a time, so every other one is reset before this one is looked for.
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

    /// <summary>
    ///     Ends whatever is running, if anything. For the effects that do not run down on their
    ///     own - the water on Olly's screen is the only one - which have to be told when the thing
    ///     they belong to has gone.
    /// </summary>
    public void StopActiveScreenEffect()
    {
        ActiveScreenEffect?.Reset();
        ActiveScreenEffect = null;
        _haltBlobAndNastiesRequested = false;
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

    /// <param name="composeOnto">
    ///     Where the finished picture goes: null for the back buffer, or the frame
    ///     <c>CrtFilter</c> is catching when the tube is switched on.
    /// </param>
    public void Draw(RenderTarget2D? composeOnto = null)
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

        _graphicsDeviceManager.GraphicsDevice.SetRenderTarget(composeOnto);
        _graphicsDeviceManager.GraphicsDevice.Clear(_presentation.ScreenClearColour);

        foreach (RenderViewPort renderViewPort in _renderViewPorts)
        {
            if (!renderViewPort.Active)
            {
                continue;
            }

            if (!IsTheGameArea(renderViewPort) || !ComposedByActiveEffect(renderViewPort))
            {
                DrawViewPortDirectly(renderViewPort);
            }

            renderViewPort.Active = false;
            renderViewPort.ComposeThrough = null;
        }
    }

    /// <summary>
    ///     Whether this is the view port the screen effects apply to. Only the playfield gets them -
    ///     the status panel and the full-screen pages are composed by
    ///     <see cref="DrawViewPortDirectly" />, with or without a shader of their own.
    /// </summary>
    private static bool IsTheGameArea(RenderViewPort renderViewPort)
        => renderViewPort.Name == ViewPorts.GameArea;

    /// <summary>
    ///     Lets the running effect compose the playfield instead, and says whether it did. An effect
    ///     that draws both halves of a transition itself leaves nothing for the plain path to do; one
    ///     that only tints what is there hands it back.
    /// </summary>
    private bool ComposedByActiveEffect(RenderViewPort renderViewPort)
        => ActiveScreenEffect != null && !ActiveScreenEffect.Draw(_renderer, renderViewPort);

    /// <summary>
    ///     One view port's render target onto the back buffer, through the presentation transform so
    ///     it lands where the composition says it should - and through whatever shader the view port
    ///     was handed by <see cref="ComposeViewPortThrough" />, if it was handed one.
    /// </summary>
    private void DrawViewPortDirectly(RenderViewPort renderViewPort)
    {
        _renderer.Begin(SpriteSortMode.Deferred, BlendState.Opaque, null, null, null,
            renderViewPort.ComposeThrough, _presentation.Transform);

        _renderer.Draw(renderViewPort.RenderTarget, new Rectangle(
            renderViewPort.XPos,
            renderViewPort.YPos,
            renderViewPort.Width,
            renderViewPort.Height), Color.White);

        _renderer.End();
    }
}
