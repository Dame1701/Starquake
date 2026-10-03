// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.GameComponents.PresentationScreens;

internal class Option(string activationKey, string optionText, bool selected, Color colour, bool unavailable = false)
{
    public readonly string ActivationKey = activationKey;
    public readonly string OptionText = optionText;
    public Color Colour = colour;
    public bool Selected = selected;

    /// <summary>
    ///     Whether the option can be chosen. Only the joypad preset ever changes after construction, and
    ///     it changes whenever a pad is plugged in or pulled out while the screen is up.
    /// </summary>
    public bool Unavailable = unavailable;

}
