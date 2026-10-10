// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Starquake.Input;

/// <summary>
///     The frame's input, read once. Mainly the keys pressed down and then released, which is what
///     the game acts on for anything that should happen once rather than while a key is held.
/// </summary>
/// <remarks>
///     An instance rather than static state, owned by <c>GameHost</c>, which is what decides when a
///     logic frame happens. <see cref="Update" /> must be called exactly once a frame from exactly
///     one place.
///     <para>
///         The snapshot it takes is public because everything reading input this frame must read the
///         same one. Separate <c>Keyboard.GetState()</c> calls can disagree inside a single frame.
///     </para>
/// </remarks>
internal class ToggleKeys
{
    private Keys _heldKey = Keys.None;
    private Keys _toggledKey = Keys.None;

    /// <summary>The keyboard and first gamepad as they were when this frame began.</summary>
    public KeyboardState KeyboardState { get; private set; }

    /// <inheritdoc cref="KeyboardState" />
    public GamePadState GamePadState { get; private set; }

    /// <summary>The key released this frame, if exactly one was down and has just come up.</summary>
    public Keys GetPressedKey() => _toggledKey;

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

    /// <summary>
    ///     The two-character label a key is shown as on the define-keys screen, where there is only
    ///     room for two. Anything without one is shown by its own name.
    /// </summary>
    /// <remarks>
    ///     Two oddities are kept deliberately, because they are what the screen shows:
    ///     <see cref="Keys.Up" /> has no entry, so it reads "Up" while the other three arrows read
    ///     DN, LF and RT; and four labels are used twice over, RT for Right and Enter, BS for browser
    ///     Search and Stop, CL for CapsLock and OemClear, and a full stop for Decimal and OemPeriod.
    /// </remarks>
    public static string GetFriendlyName(Keys key) => key switch
    {
        Keys.NumPad0 => "N0",
        Keys.NumPad1 => "N1",
        Keys.NumPad2 => "N2",
        Keys.NumPad3 => "N3",
        Keys.NumPad4 => "N4",
        Keys.NumPad5 => "N5",
        Keys.NumPad6 => "N6",
        Keys.NumPad7 => "N7",
        Keys.NumPad8 => "N8",
        Keys.NumPad9 => "N9",
        Keys.Down => "DN",
        Keys.Left => "LF",
        Keys.Right => "RT",
        Keys.RightAlt => "RA",
        Keys.LeftAlt => "LA",
        Keys.Space => "SP",
        Keys.Subtract => "-",
        Keys.Add => "+",
        Keys.CapsLock => "CL",
        Keys.Decimal => ".",
        Keys.Delete => "DL",
        Keys.Divide => "/",
        Keys.LeftControl => "LC",
        Keys.RightControl => "RC",
        Keys.RightShift => "RS",
        Keys.LeftShift => "LS",
        Keys.LeftWindows => "LW",
        Keys.RightWindows => "RW",
        Keys.Enter => "RT",
        Keys.Back => "BK",
        Keys.BrowserBack => "BB",
        Keys.BrowserFavorites => "BF",
        Keys.BrowserHome => "BH",
        Keys.BrowserRefresh => "BR",
        Keys.BrowserSearch => "BS",
        Keys.BrowserStop => "BS",
        Keys.Escape => "ES",
        Keys.Tab => "TB",
        Keys.Help => "HP",
        Keys.Home => "HM",
        Keys.Insert => "IN",
        Keys.MediaNextTrack => "MN",
        Keys.MediaPreviousTrack => "MP",
        Keys.MediaStop => "MS",
        Keys.Multiply => "*",
        Keys.PageUp => "PU",
        Keys.PageDown => "PD",
        Keys.End => "ED",
        Keys.OemBackslash => @"\",
        Keys.OemClear => "CL",
        Keys.OemComma => ",",
        Keys.OemQuestion => "?",
        Keys.OemQuotes => "\"",
        Keys.OemPeriod => ".",
        Keys.OemOpenBrackets => "{",
        Keys.OemCloseBrackets => "}",
        Keys.OemTilde => "@",
        _ => key.ToString()
    };
}
