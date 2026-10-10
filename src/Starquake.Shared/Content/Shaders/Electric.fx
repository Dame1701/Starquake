// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
// The arcs and lights of the electricity nodes and gates. Takes the drawn sprite and gives it a
// white-hot core, a coloured halo spreading out of it, and a flicker, so it reads as something
// burning rather than as a picture of a spark.
//
// It is drawn additively, as it always was, which is why black stays black here: every term is
// scaled by how bright the artwork already is at that texel, so the empty part of the sprite adds
// nothing to what is underneath it.
//
// Only the redrawn artwork goes through this. The 1985 graphics keep their own arc untouched -
// F8 is meant to show what the game looked like, not what it would look like with today's
// shaders on it.
//
// ps_2_0, because Content.mgcb is built /profile:Reach.

sampler2D SpriteTexture : register(s0);

// This frame's own square of the strip. The arc is ten frames side by side, so a tap that wanders
// out of this rectangle lands in the next frame of the animation.
float4 SpriteRect;

// How far the halo reaches, in texture coordinates, and how much of it is added back.
float2 GlowStep;
float GlowAmount;

// What the hot middle burns and what the halo around it glows.
float3 CoreColour;
float3 HaloColour;

// Where a filament starts. The artwork's band sits almost flat at an intensity of 0.89 and only
// its strands reach 0.97 and above, so anything at or below this is the body of the arc and gets
// no white at all - whitening by intensity alone lit the whole band evenly and erased the very
// contrast the strands are drawn in.
float CoreThreshold;

// How sharply the white comes on above that threshold, and how much of it there is.
float CoreSharpness;
float CoreBoost;

// This frame's brightness, a little above or below one. Taken from the logic clock, so the arc
// crackles at the same rate whatever the display is doing.
float Flicker;

float2 OnThisFrame(float2 uv)
{
    return clamp(uv, SpriteRect.xy, SpriteRect.xy + SpriteRect.zw);
}

// How hot a texel of the arc is. The largest channel, and deliberately *not* Rec. 601 luma, which
// is what Desaturate.fx uses and what this reached for first. Luma weights blue at about a ninth
// because the eye reads little brightness out of it - and this artwork is a blue arc, 67 red, 50
// green, 230 blue. Measured by luma its hottest parts came out at a third of full and the white
// core never engaged at all.
float Intensity(float3 colour)
{
    return max(colour.r, max(colour.g, colour.b));
}

float4 Electric(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float4 arc = tex2D(SpriteTexture, texCoord);

    // Four taps at the corners rather than the sides, for the reason Crt.fx gives: the sampler is
    // linear, so a corner tap returns the average of the four texels it sits among, and four of
    // them cover the ring instead of four points on it.
    float4 spill = tex2D(SpriteTexture, OnThisFrame(texCoord + GlowStep));
    spill += tex2D(SpriteTexture, OnThisFrame(texCoord - GlowStep));

    float2 across = float2(GlowStep.x, -GlowStep.y);

    spill += tex2D(SpriteTexture, OnThisFrame(texCoord + across));
    spill += tex2D(SpriteTexture, OnThisFrame(texCoord - across));
    spill *= 0.25;

    float halo = Intensity(spill.rgb);

    // Only what stands above the body of the arc counts as a filament. Rescaled so that the
    // threshold is nought and full intensity is one, then curved, so the strands take the white
    // and the band they sit in keeps its own colour.
    float filament = saturate((Intensity(arc.rgb) - CoreThreshold) / max(1.0 - CoreThreshold, 0.001));
    float core = pow(filament, CoreSharpness);

    // The halo is laid outside the arc and not inside it. Weighted by how transparent the artwork
    // is here, so where the band is solid it adds nothing and the strands drawn within it survive,
    // and where there is nothing it adds all of itself. A halo spread evenly over the sprite blurs
    // a seventeen pixel band into one smooth bar, which is what losing the strands looked like.
    float outside = 1.0 - arc.a;

    // Added to the arc rather than mixed with it. Mixing meant the dim parts of a blue arc were
    // multiplied by the halo colour, which took light *out* of the very thing this is here to
    // make brighter. Everything below only ever adds, so the arc cannot come out dimmer than the
    // artwork it started from.
    float3 lit = arc.rgb
        + CoreColour * (core * CoreBoost)
        + HaloColour * (halo * outside * GlowAmount);

    // The halo has to carry alpha with it or it will not be seen: this is drawn with source alpha
    // against one, so a bright colour where the artwork is transparent contributes nothing.
    return float4(lit * Flicker, saturate(arc.a + halo * outside * GlowAmount)) * tint;
}

technique Electric
{
    pass Burn
    {
        PixelShader = compile ps_2_0 Electric();
    }
}
