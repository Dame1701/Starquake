// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Animations;
using Starquake.Constants;
using Starquake.Fonts;

namespace Starquake.Display;

/// <summary>
///     The line in the bottom left that names what a hotkey has just switched to, fading up as the
///     key is pressed and away again a moment later.
/// </summary>
/// <remarks>
///     F8, F9 and F10 all change how the game looks without saying so, and two of them have more
///     than two settings now - the artwork swap used to be the only one where you could tell by
///     looking, and that stopped being true when F9 grew four modes.
///     <para>
///         The fade is counted in logic frames and stepped from <c>Update</c>, never from a draw.
///         <c>GameHost</c> runs the logic at fifty hertz and draws once per vertical retrace, so a
///         counter advanced while drawing runs at the display's rate - twenty per cent fast at
///         sixty hertz, nearly three times fast at 144 - and keeps running while the game is
///         paused. Ten sites in this codebase did exactly that; <c>GODCLASSES.md</c> has the list.
///     </para>
/// </remarks>
internal sealed class ModeAnnouncement
{
    /// <summary>How far in from the left of the composition the message sits.</summary>
    private const int XPos = 24;

    /// <summary>
    ///     How far the bottom of the capitals sits above the bottom of the composition. Measured to
    ///     the capitals rather than to the line, because a line box is taller than the letters in it
    ///     by an amount that belongs to the typeface - so lining the box up would leave the message
    ///     looking as though it floated at a different height from the one asked for.
    /// </summary>
    private const int BottomMargin = 20;

    /// <summary>
    ///     How long the message stays at full strength before it starts to go. Long enough to read
    ///     four words, short enough not to sit over the game.
    /// </summary>
    private static readonly int HoldFrames = LogicClock.Seconds(1.4);

    /// <summary>
    ///     Up in a fifth of a second and away over three fifths. Arriving faster than it leaves is
    ///     what makes it read as a response to the key rather than as something drifting past.
    /// </summary>
    private static readonly int FadeInStep = ColourLevels.Full / LogicClock.Seconds(0.2);

    /// <inheritdoc cref="FadeInStep" />
    private static readonly int FadeOutStep = ColourLevels.Full / LogicClock.Seconds(0.6);

    private int _fade;

    private int _framesLeftToHold;

    private string _message = string.Empty;

    /// <summary>
    ///     How strongly the message is showing, from nought to
    ///     <see cref="ColourLevels.Full" />. What <see cref="Draw" /> writes with, and what the
    ///     tests watch, the fade being the whole of this class's behaviour.
    /// </summary>
    public int Fade => _fade;

    /// <summary>
    ///     Says what has just been switched to. Replaces whatever was showing, and starts the hold
    ///     again from full, so leaning on a key reads as one message changing rather than as a
    ///     queue of them.
    /// </summary>
    public void Show(string message)
    {
        _message = message;
        _framesLeftToHold = HoldFrames;
    }

    /// <summary>
    ///     One logic frame of the fade. Safe to call every frame with nothing showing - the ramp
    ///     stops at nought and stays there, which is why there is no flag saying whether a message
    ///     is running.
    /// </summary>
    public void Update()
    {
        if (_framesLeftToHold > 0)
        {
            _framesLeftToHold--;
            _fade = Ramp.Up(_fade, FadeInStep, ColourLevels.Full);

            return;
        }

        _fade = Ramp.Down(_fade, FadeOutStep, ColourLevels.None);
    }

    /// <summary>
    ///     Draws the message over the finished picture.
    /// </summary>
    /// <remarks>
    ///     <paramref name="transform" /> is the presentation transform, and it is read rather than
    ///     applied: the font draws in the pixels of whatever it is drawing into rather than through
    ///     a matrix - <c>BitmapFont.Begin</c> takes no transform, unlike
    ///     <c>SpriteCollection.Begin</c> - so the position and the size are worked out from it
    ///     here, and the message ends up the same size against the picture whatever the window is
    ///     doing.
    /// </remarks>
    public void Draw(BitmapFont font, Matrix transform)
    {
        if (_fade <= ColourLevels.None || _message.Length == 0)
        {
            return;
        }

        float scale = transform.M11;

        // The top of the line that puts the bottom of the capitals BottomMargin above the bottom
        // of the composition.
        int lineTop = CompositionSize.Height - BottomMargin - font.CapitalTop - font.CapitalHeight;

        font.Begin(BlendState.AlphaBlend);
        font.WriteText(
            (int)(transform.Translation.X + XPos * scale),
            (int)(transform.Translation.Y + lineTop * scale),
            _message,
            0,
            Color.White,
            (byte)_fade,
            scale);
        font.End();
    }
}
