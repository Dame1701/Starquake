// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
// A nasty arriving. Two things at once: two lines open out from the middle of it leaving the nasty
// behind them, and the three colour channels start apart and converge as it settles - so it
// resolves out of a split, misaligned signal rather than simply fading up.
//
// Everything is in the sprite's own coordinates, and that is the whole reason SpriteRect exists.
// Nasties are drawn out of a sheet with their neighbours either side, so a shader that samples
// anywhere but the exact texel it was given will pull in the nasty next door. Every offset sample
// here is clamped to the rectangle this sprite actually occupies.
//
// ps_2_0, because Content.mgcb is built /profile:Reach. There is room to spare - this is three
// taps and a handful of sums, against the whole-tube pass in Crt.fx which has none.

sampler2D SpriteTexture : register(s0);

// Where this sprite sits in its sheet, in texture coordinates: xy the corner, zw the size.
float4 SpriteRect;

// Nought as the nasty begins to arrive and one once it is here.
float Progress;

// How far apart the colour channels are pulled at the start, in texture coordinates.
float2 ChromaticSpread;

// How soft the edge of the arriving line is, and how wide the hot part of it is, both as a
// fraction of the sprite's height.
float ScanSoftness;
float BandWidth;

// What colour the arriving line glows, and how hard. Over one on purpose: the line is meant to
// bloom in the cathode ray tube rather than merely be bright.
float3 BandColour;
float BandBrightness;

// The sine and cosine of the angle the sprite is being drawn at. The nasty spins while it arrives
// - two revolutions a second at its slowest - so a line sweeping down the *sprite* whirls round
// with it and reads as nothing at all. Turning the sprite's coordinates back by its own angle
// makes the line sweep down the *screen* instead, and hold still while the nasty turns under it.
float2 SpinSineCosine;

// Nothing may be sampled from outside this sprite's own square of the sheet.
float2 OnThisSprite(float2 uv)
{
    return clamp(uv, SpriteRect.xy, SpriteRect.xy + SpriteRect.zw);
}

float4 Materialise(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float2 local = (texCoord - SpriteRect.xy) / SpriteRect.zw;

    // Red and blue are pulled apart and come back together as the nasty arrives. Green is left
    // where it is, because the eye reads most of a picture's detail out of green and splitting all
    // three would smear the shape rather than fringe it.
    float2 apart = ChromaticSpread * (1.0 - Progress);

    float4 centre = tex2D(SpriteTexture, texCoord);

    float3 split = float3(
        tex2D(SpriteTexture, OnThisSprite(texCoord + apart)).r,
        centre.g,
        tex2D(SpriteTexture, OnThisSprite(texCoord - apart)).b);

    // How far down the screen this texel actually is, once the sprite's own spin is taken back
    // out of it. Half is the middle of the sprite, which is what it turns about.
    float2 centred = local - 0.5;
    float downTheScreen = centred.x * SpinSineCosine.x + centred.y * SpinSineCosine.y + 0.5;

    // How far this texel is from the middle of the sprite, measured down the screen. Two lines
    // open out from the centre rather than one crossing from the top, which is better for three
    // reasons: each travels half as far and so moves half as fast, which is most of whether it can
    // be seen at all on something spinning this quickly; it is symmetrical, so it reads the same
    // whatever angle the nasty happens to be at; and the birth already grows the sprite from
    // nothing about its centre, so a reveal opening from the centre agrees with that rather than
    // cutting across it.
    float fromTheMiddle = abs(downTheScreen - 0.5);

    // Far enough to clear the corners: a square turned on its corner reaches about seven tenths
    // from its middle, against a half for its flat edges. The softness is carried past both ends
    // so that nothing is half drawn at a progress of nought or of one - the shader stops being
    // used the moment the birth ends, and a part-drawn nasty at that moment would pop.
    float edge = Progress * (0.75 + 2.0 * ScanSoftness) - ScanSoftness;

    float arrived = 1.0 - smoothstep(edge - ScanSoftness, edge + ScanSoftness, fromTheMiddle);

    // The lines themselves: brightest where they are and gone a band either side. One expression
    // draws both, because they are the same distance out in opposite directions - and at the very
    // start they sit on top of one another, so the nasty begins as a single hot line that splits.
    float beam = saturate(1.0 - abs(fromTheMiddle - edge) / BandWidth);

    // Premultiplied all the way through - the content is built with PremultiplyAlpha - so the
    // colours carry the alpha and the two are taken down together. The beam is lit only where the
    // sprite has something to light, which is what keeps it inside the nasty's silhouette rather
    // than drawing a bar across its square.
    float3 lit = split * arrived + BandColour * (beam * BandBrightness * centre.a);

    return float4(lit, centre.a * saturate(arrived + beam)) * tint;
}

technique Materialise
{
    pass Arrive
    {
        PixelShader = compile ps_2_0 Materialise();
    }
}
