// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Input;

namespace Starquake.Input;

/// <summary>
///     This class allows checking for keys that have been pressed down and then released. This is useful for when we don't
///     want to do an action until
///     the key that has been pressed has been released
/// </summary>
public static class ToggleKeys
{
    private static Keys _pressedKey = Keys.None;
    private static Keys _toggledKey = Keys.None;

    public static Keys GetPressedKey()
    {
        var temp = _toggledKey;
        return temp;
    }

    public static void Update()
    {
        _toggledKey = Keys.None;

        if (Keyboard.GetState().GetPressedKeys().Length == 1)
        {
            _pressedKey = Keyboard.GetState().GetPressedKeys()[0];
        }

        if (_pressedKey != Keys.None)
        {
            if (Keyboard.GetState().IsKeyUp(_pressedKey))
            {
                _toggledKey = _pressedKey;
                _pressedKey = Keys.None;
            }
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