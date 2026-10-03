// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Animations;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Engines.ScreenEffects;
using Starquake.Fonts;
using Starquake.Input;

namespace Starquake.GameComponents;

/// <summary>
///     A full-screen page - the options screen, the hi-scores, game over, game complete, the
///     computer report. Each fades itself up, draws text and numbers over a cleared screen, and
///     hands over to another page when a key is pressed.
///     <para>
///         The six things held here are what every one of those five pages needs to do that, and
///         all five use all six: the sheets and the font to draw with, the graphics device to clear,
///         the presentation settings for the clear colour and the transform, the screen effect
///         engine for the fade, and the screen directory to hand over through. This used to be a
///         <c>GameEngine</c>, which let each page reach any of its thirty members instead.
///     </para>
/// </summary>
/// <remarks>
///     Five pages and not six: the five screens in <c>GameScreens</c> - the game itself, the two booths,
///     Cheops and the planet core - are not pages of text over a cleared screen and do not belong here.
///     <c>MainGameScreen</c> is the one worth saying so about, because it is the only other screen with a
///     fade of its own and so looks like a sixth. It takes five of the seven things above and not the font
///     or the graphics device, and the thirteen members it actually wants would still be coming out of the
///     engine - so it would hold both this and the engine, which is worse than holding one. The others take
///     between two and five of the seven, and a base class that fits none of its five subclasses exactly
///     is a worse answer than five constructors that each name what they use. The fade is the one thing
///     two of them really did share, and that is <see cref="Starquake.Display.ScreenFadeUp" /> now.
/// </remarks>
internal class GameScreenBase
{
    protected readonly BitmapFont Font;
    protected readonly GraphicsDeviceManager GraphicsDeviceManager;
    protected readonly PresentationSettings Presentation;
    protected readonly ResourceManager Resources;
    protected readonly ScreenDirectory ScreenDirectory;
    protected readonly ScreenEffectEngine ScreenEffects;

    /// <summary>
    ///     The frame's input, read once by <c>GameHost</c>. Every page acts on a key press, so this
    ///     is the seventh thing all of them need.
    /// </summary>
    protected readonly ToggleKeys Input;

    protected int ScreenFadeSpeed;

    protected GameScreenBase(
        ResourceManager resources,
        BitmapFont font,
        GraphicsDeviceManager graphics,
        PresentationSettings presentation,
        ScreenEffectEngine screenEffects,
        ScreenDirectory screenDirectory,
        ToggleKeys input)
    {
        Input = input;
        Resources = resources;
        Font = font;
        GraphicsDeviceManager = graphics;
        Presentation = presentation;
        ScreenEffects = screenEffects;
        ScreenDirectory = screenDirectory;
    }

    public int ScreenFade { get; set; }

    public virtual void Update(GameTime gameTime)
        => ScreenFade = Ramp.Up(ScreenFade, ScreenFadeSpeed, ColourLevels.Full);
}
