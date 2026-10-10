// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.Core;
using Starquake.Fonts;

namespace Starquake.Display;

/// <summary>
///     The line in the bottom left that names what a hotkey has just switched to, fading up as the
///     key is pressed and away again a moment later.
/// </summary>
/// <remarks>
///     F8, F9 and F10 all change how the game looks, and two of them have more than two settings, so
///     which one is in play cannot be told by looking.
///     <para>
///         The fade is counted in logic frames and stepped from <c>Update</c>, never from a draw.
///         <c>GameHost</c> runs the logic at fifty hertz and draws once per vertical retrace, so a
///         counter advanced while drawing runs at the display's rate, 20% fast at sixty hertz and
///         nearly three times fast at 144, and keeps running while the game is paused.
///         <c>GODCLASSES.md</c> lists the places that do this.
///     </para>
/// </remarks>
internal sealed class ModeAnnouncement
{
    /// <summary>How far in from the left of the composition the message sits.</summary>
    private const int XPos = 24;

    /// <summary>
    ///     How far the bottom of the capitals sits above the bottom of the composition. Measured to
    ///     the capitals rather than the line, because a line box is taller than its letters by an
    ///     amount belonging to the typeface, so aligning the box would float the message at a
    ///     different height from the one asked for.
    /// </summary>
    private const int BottomMargin = 20;

    /// <summary>
    ///     How long the message stays at full strength before fading. Long enough to read four
    ///     words, short enough not to sit over the game.
    /// </summary>
    private static readonly int HoldFrames = LogicClock.Seconds(1.4);

    /// <summary>
    ///     Up in a fifth of a second and away over three fifths. Arriving faster than it leaves is
    ///     what makes it read as a response to the key rather than something drifting past.
    /// </summary>
    private static readonly int FadeInStep = ColourLevels.Full / LogicClock.Seconds(0.2);

    /// <inheritdoc cref="FadeInStep" />
    private static readonly int FadeOutStep = ColourLevels.Full / LogicClock.Seconds(0.6);

    private int _fade;

    private int _framesLeftToHold;

    private string _message = string.Empty;

    /// <summary>
    ///     How strongly the message is showing, from nought to <see cref="ColourLevels.Full" />.
    ///     What <see cref="Draw" /> writes with.
    /// </summary>
    public int Fade => _fade;

    /// <summary>
    ///     Says what has just been switched to. Replaces whatever was showing and restarts the hold
    ///     from full, so leaning on a key reads as one message changing rather than a queue of them.
    /// </summary>
    public void Show(string message)
    {
        _message = message;
        _framesLeftToHold = HoldFrames;
    }

    /// <summary>
    ///     One logic frame of the fade. Safe to call every frame with nothing showing: the ramp stops
    ///     at nought and stays there, which is why there is no flag for whether a message is running.
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

    /// <summary>Draws the message over the finished picture.</summary>
    /// <remarks>
    ///     <paramref name="transform" /> is the presentation transform, and it is read rather than
    ///     applied. The font draws in the pixels of whatever it draws into rather than through a
    ///     matrix, since <c>BitmapFont.Begin</c> takes no transform unlike
    ///     <c>SpriteCollection.Begin</c>, so the position and size are worked out from it here. The
    ///     message then stays the same size against the picture whatever the window does.
    /// </remarks>
    public void Draw(BitmapFont font, Matrix transform)
    {
        if (_fade <= ColourLevels.None || _message.Length == 0)
        {
            return;
        }

        float scale = transform.M11;

        // The top of the line that puts the bottom of the capitals BottomMargin above the bottom of
        // the composition.
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
