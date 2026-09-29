// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Input;

namespace StarquakeXNA;

/// <summary>
///     This class allows checking for keys that have been pressed down and then released. This is useful for when we don't
///     want to do an action until
///     the key that has been pressed has been released
/// </summary>
public static class ToggleKeys
{
    private static Keys pressedKey = Keys.None;
    private static Keys toggledKey = Keys.None;

    public static Keys GetPressedKey()
    {
        var temp = toggledKey;
        return temp;
    }

    public static void Update()
    {
        toggledKey = Keys.None;

        if (Keyboard.GetState().GetPressedKeys().Length == 1)
        {
            pressedKey = Keyboard.GetState().GetPressedKeys()[0];
        }

        if (pressedKey != Keys.None)
        {
            if (Keyboard.GetState().IsKeyUp(pressedKey))
            {
                toggledKey = pressedKey;
                pressedKey = Keys.None;
            }
        }
    }

    public static string GetFriendlyName(Keys Key)
    {
        if (Key == Keys.NumPad0)
        {
            return "N0";
        }
        if (Key == Keys.NumPad1)
        {
            return "N1";
        }
        if (Key == Keys.NumPad2)
        {
            return "N2";
        }
        if (Key == Keys.NumPad3)
        {
            return "N3";
        }
        if (Key == Keys.NumPad4)
        {
            return "N4";
        }
        if (Key == Keys.NumPad5)
        {
            return "N5";
        }
        if (Key == Keys.NumPad6)
        {
            return "N6";
        }
        if (Key == Keys.NumPad7)
        {
            return "N7";
        }
        if (Key == Keys.NumPad8)
        {
            return "N8";
        }
        if (Key == Keys.NumPad9)
        {
            return "N9";
        }
        if (Key == Keys.Down)
        {
            return "DN";
        }
        if (Key == Keys.Left)
        {
            return "LF";
        }
        if (Key == Keys.Right)
        {
            return "RT";
        }
        if (Key == Keys.RightAlt)
        {
            return "RA";
        }
        if (Key == Keys.LeftAlt)
        {
            return "LA";
        }
        if (Key == Keys.Space)
        {
            return "SP";
        }
        if (Key == Keys.Subtract)
        {
            return "-";
        }
        if (Key == Keys.Add)
        {
            return "+";
        }
        if (Key == Keys.CapsLock)
        {
            return "CL";
        }
        if (Key == Keys.Decimal)
        {
            return ".";
        }
        if (Key == Keys.Delete)
        {
            return "DL";
        }
        if (Key == Keys.Divide)
        {
            return "/";
        }
        if (Key == Keys.LeftControl)
        {
            return "LC";
        }
        if (Key == Keys.RightControl)
        {
            return "RC";
        }
        if (Key == Keys.RightShift)
        {
            return "RS";
        }
        if (Key == Keys.LeftShift)
        {
            return "LS";
        }
        if (Key == Keys.LeftWindows)
        {
            return "LW";
        }
        if (Key == Keys.RightWindows)
        {
            return "RW";
        }
        if (Key == Keys.Enter)
        {
            return "RT";
        }
        if (Key == Keys.Back)
        {
            return "BK";
        }
        if (Key == Keys.BrowserBack)
        {
            return "BB";
        }
        if (Key == Keys.BrowserFavorites)
        {
            return "BF";
        }
        if (Key == Keys.BrowserHome)
        {
            return "BH";
        }
        if (Key == Keys.BrowserRefresh)
        {
            return "BR";
        }
        if (Key == Keys.BrowserSearch)
        {
            return "BS";
        }
        if (Key == Keys.BrowserStop)
        {
            return "BS";
        }
        if (Key == Keys.Escape)
        {
            return "ES";
        }
        if (Key == Keys.Tab)
        {
            return "TB";
        }
        if (Key == Keys.Help)
        {
            return "HP";
        }
        if (Key == Keys.Home)
        {
            return "HM";
        }
        if (Key == Keys.Insert)
        {
            return "IN";
        }
        if (Key == Keys.MediaNextTrack)
        {
            return "MN";
        }
        if (Key == Keys.MediaPreviousTrack)
        {
            return "MP";
        }
        if (Key == Keys.MediaStop)
        {
            return "MS";
        }
        if (Key == Keys.Multiply)
        {
            return "*";
        }
        if (Key == Keys.PageUp)
        {
            return "PU";
        }
        if (Key == Keys.PageDown)
        {
            return "PD";
        }
        if (Key == Keys.End)
        {
            return "ED";
        }
        if (Key == Keys.OemBackslash)
        {
            return @"\";
        }
        if (Key == Keys.OemClear)
        {
            return "CL";
        }
        if (Key == Keys.OemComma)
        {
            return ",";
        }
        if (Key == Keys.OemQuestion)
        {
            return "?";
        }
        if (Key == Keys.OemQuotes)
        {
            return "\"";
        }
        if (Key == Keys.OemPeriod)
        {
            return ".";
        }
        if (Key == Keys.OemOpenBrackets)
        {
            return "{";
        }
        if (Key == Keys.OemCloseBrackets)
        {
            return "}";
        }
        if (Key == Keys.OemTilde)
        {
            return "@";
        }
        return Key.ToString();
    }
}