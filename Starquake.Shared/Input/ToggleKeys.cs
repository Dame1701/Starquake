using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;

namespace StarquakeXNA
{
    /// <summary>
    /// This class allows checking for keys that have been pressed down and then released. This is useful for when we don't want to do an action until
    /// the key that has been pressed has been released
    /// </summary>
    public static class ToggleKeys
    {
        private static Keys pressedKey = Keys.None;
        private static Keys toggledKey = Keys.None;

        public static Keys GetPressedKey()
        {
            Keys temp = toggledKey;
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
            else if (Key == Keys.NumPad1)
            {
                return "N1";
            }
            else if (Key == Keys.NumPad2)
            {
                return "N2";
            }
            else if (Key == Keys.NumPad3)
            {
                return "N3";
            }
            else if (Key == Keys.NumPad4)
            {
                return "N4";
            }
            else if (Key == Keys.NumPad5)
            {
                return "N5";
            }
            else if (Key == Keys.NumPad6)
            {
                return "N6";
            }
            else if (Key == Keys.NumPad7)
            {
                return "N7";
            }
            else if (Key == Keys.NumPad8)
            {
                return "N8";
            }
            else if (Key == Keys.NumPad9)
            {
                return "N9";
            }
            else if (Key == Keys.Down)
            {
                return "DN";
            }
            else if (Key == Keys.Left)
            {
                return "LF";
            }
            else if (Key == Keys.Right)
            {
                return "RT";
            }
            else if (Key == Keys.RightAlt)
            {
                return "RA";
            }
            else if (Key == Keys.LeftAlt)
            {
                return "LA";
            }
            else if (Key == Keys.Space)
            {
                return "SP";
            }
            else if (Key == Keys.Subtract)
            {
                return "-";
            }
            else if (Key == Keys.Add)
            {
                return "+";
            }
            else if (Key == Keys.CapsLock)
            {
                return "CL";
            }
            else if (Key == Keys.Decimal)
            {
                return ".";
            }
            else if (Key == Keys.Delete)
            {
                return "DL";
            }
            else if (Key == Keys.Divide)
            {
                return "/";
            }
            else if (Key == Keys.LeftControl)
            {
                return "LC";
            }
            else if (Key == Keys.RightControl)
            {
                return "RC";
            }
            else if (Key == Keys.RightShift)
            {
                return "RS";
            }
            else if (Key == Keys.LeftShift)
            {
                return "LS";
            }
            else if (Key == Keys.LeftWindows)
            {
                return "LW";
            }
            else if (Key == Keys.RightWindows)
            {
                return "RW";
            }
            else if (Key == Keys.Enter)
            {
                return "RT";
            }
            else if (Key == Keys.Back)
            {
                return "BK";
            }
            else if (Key == Keys.BrowserBack)
            {
                return "BB";
            }
            else if (Key == Keys.BrowserFavorites)
            {
                return "BF";
            }
            else if (Key == Keys.BrowserHome)
            {
                return "BH";
            }
            else if (Key == Keys.BrowserRefresh)
            {
                return "BR";
            }
            else if (Key == Keys.BrowserSearch)
            {
                return "BS";
            }
            else if (Key == Keys.BrowserStop)
            {
                return "BS";
            }
            else if (Key == Keys.Escape)
            {
                return "ES";
            }
            else if (Key == Keys.Tab)
            {
                return "TB";
            }
            else if (Key == Keys.Help)
            {
                return "HP";
            }
            else if (Key == Keys.Home)
            {
                return "HM";
            }
            else if (Key == Keys.Insert)
            {
                return "IN";
            }
            else if (Key == Keys.MediaNextTrack)
            {
                return "MN";
            }
            else if (Key == Keys.MediaPreviousTrack)
            {
                return "MP";
            }
            else if (Key == Keys.MediaStop)
            {
                return "MS";
            }
            else if (Key == Keys.Multiply)
            {
                return "*";
            }
            else if (Key == Keys.PageUp)
            {
                return "PU";
            }
            else if (Key == Keys.PageDown)
            {
                return "PD";
            }
            else if (Key == Keys.End)
            {
                return "ED";
            }
            else if (Key == Keys.OemBackslash)
            {
                return @"\";
            }
            else if (Key == Keys.OemClear)
            {
                return "CL";
            }
            else if (Key == Keys.OemComma)
            {
                return ",";
            }
            else if (Key == Keys.OemQuestion)
            {
                return "?";
            }
            else if (Key == Keys.OemQuotes)
            {
                return "\"";
            }
            else if (Key == Keys.OemPeriod)
            {
                return ".";
            }
            else if (Key == Keys.OemOpenBrackets)
            {
                return "{";
            }
            else if (Key == Keys.OemCloseBrackets)
            {
                return "}";
            }
            else if (Key == Keys.OemTilde)
            {
                return "@";
            }
            else return Key.ToString();
        }
    }
}
