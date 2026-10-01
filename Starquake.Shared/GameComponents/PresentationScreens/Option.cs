// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.GameComponents.PresentationScreens;

internal class Option
{
    public readonly string ActivationKey;
    public readonly string OptionText;
    public Color Colour;
    public bool Selected;

    /// <summary>
    ///     Whether the option can be chosen. Only the joypad preset ever changes after construction, and
    ///     it changes whenever a pad is plugged in or pulled out while the screen is up.
    /// </summary>
    public bool Unavailable;

    public Option(string activationKey, string optionText, bool selected, Color colour, bool unavailable = false)
    {
        ActivationKey = activationKey;
        OptionText = optionText;
        Selected = selected;
        Colour = colour;
        Unavailable = unavailable;
    }
}
