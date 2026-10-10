// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
// A jump platform coming apart. The dissolve itself is not here - SpritePixelMatrix rubs out fifty
// random pixels a frame and the platform is composed into a render target with those left
// transparent. What this adds is what happens at the holes.
//
// The artwork is a dark slab - a mean of 45, 33, 34 and a median intensity under a fifth - so
// dark pixels being replaced by nothing is nearly invisible. Every edge the dissolve opens burns
// instead, which turns a quiet erosion into something visibly eating through, and the whole slab
// grows more charged and less steady as it goes.
//
// The render target is one to one with the sprite and drawn whole, so there is no sheet to stray
// into and no rectangle to clamp against - unlike every other sprite shader here.
//
// ps_2_0, because Content.mgcb is built /profile:Reach.

sampler2D SpriteTexture : register(s0);

// One texel of the render target, for looking at what is next to this one.
float2 TexelSize;

// Nought while the platform stands whole and one as the last of it goes.
float Decay;

// This frame's brightness, a little above or below one, from the logic clock. It bites harder as
// the platform decays, so a fresh one is steady and a failing one is not.
float Flicker;

// What a cut edge burns, and how hard.
float3 EdgeColour;
float EdgeBoost;

// How much the whole slab lifts toward that colour as it goes, so it reads as charging up to
// failure rather than simply being eaten.
float Charge;

float4 Dissolving(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float4 slab = tex2D(SpriteTexture, texCoord);

    // What is left around this texel. The dissolve takes single pixels at random, so a texel
    // beside a hole has three solid neighbours and one gone - which is enough to find it.
    float around = tex2D(SpriteTexture, texCoord + float2(TexelSize.x, 0)).a;
    around += tex2D(SpriteTexture, texCoord - float2(TexelSize.x, 0)).a;
    around += tex2D(SpriteTexture, texCoord + float2(0, TexelSize.y)).a;
    around += tex2D(SpriteTexture, texCoord - float2(0, TexelSize.y)).a;
    around *= 0.25;

    // Solid here and less so around means this texel is on the lip of a hole. Doubled because a
    // single missing neighbour of four only moves it a quarter.
    float cut = saturate((slab.a - around) * 2.0);

    // Hotter the further gone the platform is, and unsteady with it.
    float burn = cut * EdgeBoost * (0.4 + Decay) * Flicker;

    float3 lit = slab.rgb + EdgeColour * (burn + Charge * Decay * slab.a);

    return float4(lit, slab.a) * tint;
}

technique Dissolving
{
    pass ComeApart
    {
        PixelShader = compile ps_2_0 Dissolving();
    }
}
