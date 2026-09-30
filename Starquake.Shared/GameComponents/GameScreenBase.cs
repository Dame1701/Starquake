// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Engines.ScreenEffects;
using Starquake.Fonts;
using Starquake.Structure;

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
public class GameScreenBase : GameComponentBase
{
    protected readonly BitmapFont Font;
    protected readonly GraphicsDeviceManager GraphicsDeviceManager;
    protected readonly PresentationSettings Presentation;
    protected readonly ResourceManager Resources;
    protected readonly ScreenDirectory ScreenDirectory;
    protected readonly ScreenEffectEngine ScreenEffects;

    protected int ScreenFadeSpeed;

    protected GameScreenBase(
        ResourceManager resources,
        BitmapFont font,
        GraphicsDeviceManager graphics,
        PresentationSettings presentation,
        ScreenEffectEngine screenEffects,
        ScreenDirectory screenDirectory)
    {
        Resources = resources;
        Font = font;
        GraphicsDeviceManager = graphics;
        Presentation = presentation;
        ScreenEffects = screenEffects;
        ScreenDirectory = screenDirectory;
    }

    public int ScreenFade { get; set; }

    public override void Update(GameTime gameTime)
    {
        // Fade screen back up
        if (ScreenFade < ColourLevels.Full)
        {
            ScreenFade = ScreenFade + ScreenFadeSpeed;
        }

        if (ScreenFade > ColourLevels.Full)
        {
            ScreenFade = ColourLevels.Full;
        }
    }
}
