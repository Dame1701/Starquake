// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.GameComponents.GameScreens;

public class TeleportCodeLetter
{

    public TeleportCodeLetter(string letter, Point location)
    {
        this.Letter = letter;
        this.Location = location;
    }

    public string Letter { get; set; }

    public Point Location { get; set; }
}