// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.GameComponents.PresentationScreens;

public class Option
{
    public readonly string ActivationKey;
    public Color Colour;
    public readonly string OptionText;
    public bool Selected;
    public readonly bool Unavailable;

    public Option(string activationKey, string optionText, bool selected, Color colour, bool unavailable = false)
    {
        this.ActivationKey = activationKey;
        this.OptionText = optionText;
        this.Selected = selected;
        this.Colour = colour;
        this.Unavailable = unavailable;
    }
}