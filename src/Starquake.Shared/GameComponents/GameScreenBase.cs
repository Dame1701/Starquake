// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.Engines.ScreenEffects;
using Starquake.Fonts;
using Starquake.Input;

namespace Starquake.GameComponents;

/// <summary>
///     A full-screen page: the options screen, the hi-scores, game over, game complete, the computer
///     report. Each fades itself up, draws text and numbers over a cleared screen, and hands over to
///     another page when a key is pressed - and three of them when their music ends, so the end of a
///     game plays itself out to the title screen whether or not anyone is watching.
///     <para>
///         Everything held here is what all five of those pages need: the sheets and the font to draw
///         with, the graphics device to clear, the presentation settings for the clear colour and the
///         transform, the screen effect engine for the fade, and the screen directory to hand over
///         through.
///     </para>
/// </summary>
/// <remarks>
///     Five pages and not six. The screens in <c>GameScreens</c> are not pages of text over a cleared
///     screen and do not belong here. <c>MainGameScreen</c> is the one worth saying so about, because
///     it is the only other screen with a fade of its own and so looks like a sixth, but it wants
///     neither the font nor the graphics device. A base class fitting none of its subclasses exactly
///     is worse than constructors that each name what they use.
/// </remarks>
internal class GameScreenBase
{
    protected readonly BitmapFont Font;
    protected readonly GraphicsDeviceManager GraphicsDeviceManager;

    /// <summary>
    ///     The frame's input, read once by <c>GameHost</c>. Every page acts on a key press.
    /// </summary>
    protected readonly ToggleKeys Input;
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
