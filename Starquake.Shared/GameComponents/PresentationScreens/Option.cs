// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.GameComponents.PresentationScreens;

public class Option
{
    public readonly string ActivationKey;
    public readonly string OptionText;
    public readonly bool Unavailable;
    public Color Colour;
    public bool Selected;

    public Option(string activationKey, string optionText, bool selected, Color colour, bool unavailable = false)
    {
        ActivationKey = activationKey;
        OptionText = optionText;
        Selected = selected;
        Colour = colour;
        Unavailable = unavailable;
    }
}
