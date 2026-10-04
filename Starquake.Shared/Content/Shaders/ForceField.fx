// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
// The force field across a key-locked door, drawn out of no artwork at all.
//
// Tile 419 is the door, and nothing is painted on it - DrawLowerTiles stops short of it on
// purpose, so the gap in the wall is empty and whatever fills it is light. It used to be filled
// by particles alone: a column of lens flares, grey at the jambs and red or green down the
// middle. That reads as sparks in a doorway rather than as something barring it, because sparks
// have gaps and a barrier may not have any.
//
// So the sheet is worked out here and the particles are left on top of it as the crackle they
// always were. The same pass serves both states, because the difference between them is not a
// colour: a locked door is a sheet drawn right across the gap, and an open one is that sheet
// pulled back onto its jambs leaving the middle clear to walk through. DrawBack is which.
//
// ps_2_0, because Content.mgcb is built /profile:Reach.

// What the field burns. One colour rather than two, because the red and the green are crossfaded
// on the processor as the door opens - see ForceFieldSettings.
float3 Colour;

// How hard the whole thing burns. Carries the flicker and the bracing against Blob leaning on a
// locked door, both of which are worked out per frame rather than per pixel.
float Intensity;

// How far the sheet is pulled back onto the jambs. Under one leaves it lit all the way across,
// which is a closed door; over one empties the middle, which is an open one.
float DrawBack;

// How closely the travelling bands are spaced down the gap, and where they have got to. The
// phase comes off the logic clock, so the field moves at the same rate whatever the display is
// doing.
float Frequency;
float Phase;

// The scanner: where the bar has got to down the gap, and the reciprocal of how thick it is. The
// divide is done on the processor because the thickness never changes between frames - see
// BUILD.md, which has the two ways of buying slots back and this is one of them.
float ScanAt;
float OverScanWidth;

// How hard the jambs burn. They are lit whatever the sheet is doing, so an open door is still
// plainly a door rather than an empty gap.
float PostGlow;

// The burst at the moment the door gives way. Nought for all but a fraction of a second.
float Flash;

float4 Field(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    // The quad is the doorway itself, so nothing is sampled: x runs from one jamb to the other
    // and y from the lintel to the floor.
    float across = (texCoord.x - 0.5) * 2.0;

    // One in the middle of the gap, nought at either jamb.
    float fromPost = 1.0 - abs(across);

    float sheet = saturate(1.0 - fromPost * DrawBack);

    // Two bands travelling at rates that do not divide one another, and in opposite directions,
    // so the field reads as energy held between the posts rather than as a stripe rolling past.
    // One sine alone is a barber's pole.
    float bands = sin(texCoord.y * Frequency + Phase)
        + 0.5 * sin(texCoord.y * Frequency * 2.7 - Phase * 1.3);

    // Brought into nought to one and kept off the floor, or the sheet goes out between bands and
    // a barrier that blinks out is not a barrier. The floor is low enough to leave the bands
    // plainly darker than the light between them: a sheet of even brightness is a painted
    // rectangle, which is what the first attempt at this looked like.
    bands = 0.25 + 0.75 * saturate(bands * 0.5 + 0.5);

    // The scanner working down the gap. Squared so it is a bar rather than a wash, and taken
    // through the sheet so that an open door scans only what is left of it - which is the jambs,
    // and is why this needs no second knob to turn it off.
    float scan = saturate(1.0 - abs(texCoord.y - ScanAt) * OverScanWidth);
    scan *= scan * sheet;

    // The jambs the field is anchored to. Raised by multiplying rather than with pow, which
    // compiles to a logarithm, a multiply and an exponent.
    float posts = across * across;
    posts *= posts;

    // The sheet is coloured and the two bright things are not. A scanner and a door giving way
    // are both the field at full power, and a field at full power goes white - running them
    // through the colour instead gave a red bar on red, which is no bar at all.
    float lit = sheet * bands + posts * PostGlow;
    float hot = scan + Flash;

    return float4((Colour * lit + hot) * Intensity, saturate((lit + hot) * Intensity)) * tint;
}

technique Field
{
    pass Bar
    {
        PixelShader = compile ps_2_0 Field();
    }
}
