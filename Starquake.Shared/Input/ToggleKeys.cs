// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Starquake.Input;

/// <summary>
///     The frame's input, read once. Its main job is the keys that have been pressed down and then
///     released, which is what the game acts on for anything that should happen once rather than for
///     as long as a key is held.
/// </summary>
/// <remarks>
///     This was two mutable static fields, the last global state in the game - it survived the
///     deletion of GameEngine.GetInstance() in stage 2 of DECOUPLING.md. It required Update to be
///     called exactly once a frame from exactly one place and nothing in the type system said so.
///     It is an instance now, owned by GameHost, which is the thing that decides when a logic frame
///     happens.
///     <para>
///         The snapshot Update takes is public because everything reading input this frame should
///         read the same one. Keyboard.GetState() was called three times here and five more in
///         CustomControl, so two of those reads could disagree inside one frame.
///     </para>
/// </remarks>
public class ToggleKeys
{
    private Keys _heldKey = Keys.None;
    private Keys _toggledKey = Keys.None;

    /// <summary>
    ///     The keyboard and first gamepad as they were when this frame began.
    /// </summary>
    public KeyboardState KeyboardState { get; private set; }

    /// <inheritdoc cref="KeyboardState" />
    public GamePadState GamePadState { get; private set; }

    /// <summary>
    ///     The key released this frame, if exactly one was down and has just come up.
    /// </summary>
    public Keys GetPressedKey()
        => _toggledKey;

    public void Update()
    {
        KeyboardState = Keyboard.GetState();
        GamePadState = GamePad.GetState(PlayerIndex.One);

        _toggledKey = Keys.None;

        Keys[] pressedKeys = KeyboardState.GetPressedKeys();

        if (pressedKeys.Length == 1)
        {
            _heldKey = pressedKeys[0];
        }

        if (_heldKey != Keys.None && KeyboardState.IsKeyUp(_heldKey))
        {
            _toggledKey = _heldKey;
            _heldKey = Keys.None;
        }
    }

    public static string GetFriendlyName(Keys key)
    {
        if (key == Keys.NumPad0)
        {
            return "N0";
        }
        if (key == Keys.NumPad1)
        {
            return "N1";
        }
        if (key == Keys.NumPad2)
        {
            return "N2";
        }
        if (key == Keys.NumPad3)
        {
            return "N3";
        }
        if (key == Keys.NumPad4)
        {
            return "N4";
        }
        if (key == Keys.NumPad5)
        {
            return "N5";
        }
        if (key == Keys.NumPad6)
        {
            return "N6";
        }
        if (key == Keys.NumPad7)
        {
            return "N7";
        }
        if (key == Keys.NumPad8)
        {
            return "N8";
        }
        if (key == Keys.NumPad9)
        {
            return "N9";
        }
        if (key == Keys.Down)
        {
            return "DN";
        }
        if (key == Keys.Left)
        {
            return "LF";
        }
        if (key == Keys.Right)
        {
            return "RT";
        }
        if (key == Keys.RightAlt)
        {
            return "RA";
        }
        if (key == Keys.LeftAlt)
        {
            return "LA";
        }
        if (key == Keys.Space)
        {
            return "SP";
        }
        if (key == Keys.Subtract)
        {
            return "-";
        }
        if (key == Keys.Add)
        {
            return "+";
        }
        if (key == Keys.CapsLock)
        {
            return "CL";
        }
        if (key == Keys.Decimal)
        {
            return ".";
        }
        if (key == Keys.Delete)
        {
            return "DL";
        }
        if (key == Keys.Divide)
        {
            return "/";
        }
        if (key == Keys.LeftControl)
        {
            return "LC";
        }
        if (key == Keys.RightControl)
        {
            return "RC";
        }
        if (key == Keys.RightShift)
        {
            return "RS";
        }
        if (key == Keys.LeftShift)
        {
            return "LS";
        }
        if (key == Keys.LeftWindows)
        {
            return "LW";
        }
        if (key == Keys.RightWindows)
        {
            return "RW";
        }
        if (key == Keys.Enter)
        {
            return "RT";
        }
        if (key == Keys.Back)
        {
            return "BK";
        }
        if (key == Keys.BrowserBack)
        {
            return "BB";
        }
        if (key == Keys.BrowserFavorites)
        {
            return "BF";
        }
        if (key == Keys.BrowserHome)
        {
            return "BH";
        }
        if (key == Keys.BrowserRefresh)
        {
            return "BR";
        }
        if (key == Keys.BrowserSearch)
        {
            return "BS";
        }
        if (key == Keys.BrowserStop)
        {
            return "BS";
        }
        if (key == Keys.Escape)
        {
            return "ES";
        }
        if (key == Keys.Tab)
        {
            return "TB";
        }
        if (key == Keys.Help)
        {
            return "HP";
        }
        if (key == Keys.Home)
        {
            return "HM";
        }
        if (key == Keys.Insert)
        {
            return "IN";
        }
        if (key == Keys.MediaNextTrack)
        {
            return "MN";
        }
        if (key == Keys.MediaPreviousTrack)
        {
            return "MP";
        }
        if (key == Keys.MediaStop)
        {
            return "MS";
        }
        if (key == Keys.Multiply)
        {
            return "*";
        }
        if (key == Keys.PageUp)
        {
            return "PU";
        }
        if (key == Keys.PageDown)
        {
            return "PD";
        }
        if (key == Keys.End)
        {
            return "ED";
        }
        if (key == Keys.OemBackslash)
        {
            return @"\";
        }
        if (key == Keys.OemClear)
        {
            return "CL";
        }
        if (key == Keys.OemComma)
        {
            return ",";
        }
        if (key == Keys.OemQuestion)
        {
            return "?";
        }
        if (key == Keys.OemQuotes)
        {
            return "\"";
        }
        if (key == Keys.OemPeriod)
        {
            return ".";
        }
        if (key == Keys.OemOpenBrackets)
        {
            return "{";
        }
        if (key == Keys.OemCloseBrackets)
        {
            return "}";
        }
        if (key == Keys.OemTilde)
        {
            return "@";
        }
        return key.ToString();
    }
}
