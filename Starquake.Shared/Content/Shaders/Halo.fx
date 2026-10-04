// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
// The halo around a shuttle tag - the lights that mark where a pad used to be.
//
// It reads no texture. The tile is drawn two and a half times its own size purely to give this
// something to work on, and every pixel of the glow is worked out from where it falls within that
// square. Trying to do this with Electric.fx first is what showed why it needs its own: that one
// spreads light from bright texels with four taps, which is a rim at four pixels and four ghost
// copies at twenty, where a halo wants a smooth falloff the size of the tile again.
//
// ps_2_0, because Content.mgcb is built /profile:Reach.

sampler2D SpriteTexture : register(s0);

// The square the tile is drawn in, within its sheet. Not to read from - nothing here samples - but
// to turn a point in the sheet into a point in the halo.
float4 SpriteRect;

// How far the glow reaches as a fraction of that square, as a reciprocal, and how sharply it falls
// away. The divide is done on the processor because it never changes between frames.
float OverReach;
float Falloff;

// The quad's height over its width. The glow is measured in the quad's own coordinates, which run
// nought to one on both axes however long the sides actually are - so without this a quad that is
// wider than it is tall gives an oval rather than a halo.
float YOverX;

float4 Halo(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float2 fromMiddle = (texCoord - SpriteRect.xy) / SpriteRect.zw - 0.5;

    fromMiddle.y *= YOverX;

    // Squared rather than a true distance: it holds the middle brighter and saves a square root,
    // and the shape of the falloff is what matters here rather than its units.
    float out2 = saturate(dot(fromMiddle, fromMiddle) * 4.0 * OverReach);

    float glow = 1.0 - out2;

    // Raised, so it is a soft core bleeding away rather than an even disc. Written as multiplies
    // because pow compiles to a logarithm, a multiply and an exponent.
    glow = glow * glow;
    glow = lerp(glow, glow * glow, Falloff);

    return float4(glow, glow, glow, glow) * tint;
}

technique Halo
{
    pass Glow
    {
        PixelShader = compile ps_2_0 Halo();
    }
}
