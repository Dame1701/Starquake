// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
// The screen Olly sits on, flooded. Everything below the waterline is read through moving water -
// displaced, tinted, and dimmed with depth - and the surface itself is a bright moving line.
//
// It refracts rather than tints, which is the whole of why it is a screen effect and not an
// overlay: what sells water is the room behind it moving, and nothing drawn over the top can do
// that. The playfield is already composed into a render target by the time this runs, so it is
// there to be read.
//
// ps_2_0, because Content.mgcb is built /profile:Reach.

sampler2D SpriteTexture : register(s0);

// Where the surface sits, as a fraction down the picture, and the reciprocal of how far below it
// the water reaches its full colour. The divide is done on the processor because it never changes
// between frames - see BUILD.md.
float Waterline;
float OverFullDepth;

// Where the waves have travelled to. From the logic clock, so the water moves at the same rate
// whatever the display is doing.
float Phase;

// How far the water pushes what is behind it, as a fraction of the picture, and how closely the
// waves are spaced along the surface.
float Amplitude;
float Frequency;

// What the water is, how much of it is laid over what is behind, and what the surface burns.
float3 WaterColour;
float Tint;
float3 SurfaceColour;

// How thick the bright line at the surface is, as a fraction of the picture.
float SurfaceThickness;

float4 Flooded(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    // How far below the surface this pixel is. Negative above it, which is the air.
    float below = texCoord.y - Waterline;

    // Two waves at rates that do not divide one another, so the surface does not read as a
    // repeating ripple. The deeper the pixel the less it is pushed, as the real thing is.
    float wave = sin(texCoord.y * Frequency + Phase)
        + 0.5 * sin(texCoord.x * Frequency * 0.7 - Phase * 1.3);

    float deep = saturate(below * OverFullDepth);
    float pushed = wave * Amplitude * (1.0 - deep * 0.6);

    // Only what is under the water moves. step rather than a branch: ps_2_0 has none.
    float under = step(0.0, below);

    float2 uv = saturate(float2(texCoord.x + pushed * under, texCoord.y));

    float4 room = tex2D(SpriteTexture, uv);

    // Greener and darker with depth, the way looking down through water goes.
    float3 lit = lerp(room.rgb, WaterColour * room.rgb, Tint * deep * under);
    lit *= 1.0 - deep * 0.35 * under;

    // The surface itself, which moves with the waves rather than lying flat.
    float surface = saturate(1.0
        - abs(below - wave * Amplitude) / max(SurfaceThickness, 0.001));

    return float4(lit + SurfaceColour * (surface * surface), room.a) * tint;
}

technique Flooded
{
    pass Submerge
    {
        PixelShader = compile ps_2_0 Flooded();
    }
}
