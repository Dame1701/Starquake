// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
// An organic nasty coming apart. The body is eaten away from its edges inward along a lumpy
// boundary, with a wet rim glowing in whatever colour that kind of nasty splatters, so it reads as
// something fleshy giving way rather than a sprite being faded out.
//
// Only the organic ones. Spiky and conveyor nasties are machines and keep the explosion they have
// always had - Nasty.DrawDeath picks between the two, and the same test picks their particles.
//
// ps_2_0, because Content.mgcb is built /profile:Reach.

sampler2D SpriteTexture : register(s0);

// Where this sprite sits in its sheet, in texture coordinates: xy the corner, zw the size. The
// nasties share a sheet, so nothing may be read outside this rectangle or it would be the nasty
// next door showing through the hole.
float4 SpriteRect;

// Nought the moment it is shot, one once there is nothing left.
float Decay;

// How soft the eaten edge is, and how wide the wet rim on it is.
float EdgeSoftness;
float RimWidth;

// What the rim glows, and how hard. Over one on purpose: it is meant to bloom in the tube.
float3 RimColour;
float RimBrightness;

// How much the boundary wanders. At nought the body dissolves as a clean oval.
float Lumpiness;

float4 Splat(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float4 flesh = tex2D(SpriteTexture, texCoord);

    float2 local = (texCoord - SpriteRect.xy) / SpriteRect.zw;
    float2 fromMiddle = local - 0.5;

    // Two sine waves crossing at frequencies that do not divide one another make a smooth, blobby
    // field. A hash would be cheaper and is wrong: it gives television static, and flesh does not
    // come apart in single pixels.
    float lumps = sin(local.x * 11.0 + local.y * 7.0) + sin(local.y * 17.0 - local.x * 6.0);

    // Squared rather than a true distance, which holds the middle of the body longer and saves a
    // square root. One at the centre, nought by the edge.
    float holds = 1.0 - saturate(dot(fromMiddle, fromMiddle) * 4.0);

    float field = holds + lumps * Lumpiness;

    // The field runs a little beyond nought and one once the lumps are added, and the softness has
    // to clear both ends - so the threshold is swept wider than the field it is compared against.
    // Otherwise the body would be part eaten the moment it was shot, and a scrap of it would still
    // be there when the corpse stops being drawn.
    float threshold = Decay * (1.7 + 2.0 * EdgeSoftness) - 0.35 - EdgeSoftness;

    float remaining = smoothstep(threshold - EdgeSoftness, threshold + EdgeSoftness, field);

    // The boundary itself, where the flesh is giving way.
    float rim = saturate(1.0 - abs(field - threshold) / RimWidth);

    // Premultiplied throughout - the content is built with PremultiplyAlpha - so the colours carry
    // the alpha and the two come down together. The rim is lit only where the sprite has something
    // to light, which keeps it inside the nasty rather than drawing a shape in its empty corners.
    float3 lit = flesh.rgb * remaining + RimColour * (rim * RimBrightness * flesh.a);

    return float4(lit, flesh.a * saturate(remaining + rim)) * tint;
}

technique Splat
{
    pass ComeApart
    {
        PixelShader = compile ps_2_0 Splat();
    }
}
