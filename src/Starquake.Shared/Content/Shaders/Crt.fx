// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
// The CRT tube. Takes the finished picture as a texture and gives it back as something a
// cathode ray tube might have produced: scanlines, a phosphor grille, a glow and a curve.
//
// Why this is not the bitmap it replaces. The old overlay was a 1280x800 texture of alternating
// rows - clear, then black at half alpha - blended over the picture through the presentation
// transform. That transform is a uniform float scale, min(backBufferW / 1280, backBufferH / 800),
// and it is essentially never a whole number: 1.35 at 1080p, 1.8 at 1440p, 2.7 on a 16in laptop.
// So a two-pixel pattern was resampled at a fractional rate with bilinear filtering, which
// averages the rows toward a flat grey wash and leaves a slow beat wherever the phase drifts.
// Here the pattern is computed from the output pixel instead, so its period is a whole number of
// physical pixels and cannot drift at all.
//
// Everything is ps_2_0, because Content.mgcb is built /profile:Reach and allows no higher. That
// is the reason for three techniques rather than one shader with branches: ps_2_0 has no dynamic
// branching and 64 arithmetic slots, so each mode is compiled as only the work it needs. It is
// also why the glow is four taps and the tests below are step() products rather than ifs.

Texture2D SpriteTexture : register(t0);
SamplerState SpriteTextureSampler : register(s0);

// The back buffer in pixels. Turns a texture coordinate into the output pixel the scanlines and
// the grille have to line up with.
float2 BackBufferSize;

// Where the composition sits inside the back buffer, in texture coordinates: xy is the corner and
// zw the size. The picture is letterboxed, and the curve has to bow the picture within its own
// frame rather than within the whole buffer.
float4 CompositionRect;

// How many output pixels one light-and-dark pair spans. Worked out on the processor so it is a
// whole number; see CrtSettings.ScanlinePeriodFor.
float ScanlinePeriod;

// How dark the trough of a scanline goes, 0 for none and 1 for black.
float ScanlineDepth;

// What the picture is multiplied by to make up for the light the scanlines and the grille take
// out of it. Also computed on the processor, so the two cannot disagree.
float Gain;

// One red, green and blue stripe span, in output pixels, and how deep the stripes cut.
float MaskPeriod;
float MaskDepth;

// How far the glow reaches, in texture coordinates, and how much of it is added back.
float2 GlowStep;
float GlowAmount;

// How much the tube bows. 0 is flat.
float Curvature;

// How much the corners fall away.
float VignetteAmount;

// ---------------------------------------------------------------------------------------------
// The pieces, each written once and shared by the techniques that want it.
// ---------------------------------------------------------------------------------------------

// The scanline multiplier for one output pixel. A triangle wave across the period, smoothed, so
// the dark band has a soft edge rather than being a hard stripe - which is most of the difference
// between reading as a tube and reading as a pattern laid on top.
float Scanline(float outputY)
{
    float phase = frac(outputY / ScanlinePeriod);
    float wave = abs(phase * 2.0 - 1.0);

    return lerp(1.0 - ScanlineDepth, 1.0, smoothstep(0.0, 1.0, wave));
}

// The phosphor grille. Each channel peaks a third of a period apart, so a white pixel breaks into
// red, green and blue stripes the way an aperture grille does.
float3 Grille(float outputX)
{
    float3 phase = frac(outputX / MaskPeriod + float3(0.0, 0.3333333, 0.6666667));

    return lerp(1.0 - MaskDepth, 1.0, abs(phase * 2.0 - 1.0));
}

// The beam spilling into its neighbours. Added rather than mixed, so it lifts the bright parts
// and leaves the dark ones alone - a dark pixel has no beam to spill.
//
// Four taps, placed at the corners rather than at the sides. Two reasons, and the first is shape:
// four taps on the axes make a cross, and bright things grew spikes rather than halos. The second
// is that the sampler is LinearClamp, so a tap landing between texels returns the average of the
// four it sits among - the corners each cover a 2x2 neighbourhood, and these four between them
// cover the ring rather than four points on it.
//
// Eight taps - the sides as well - was tried and is what the whole-tube pass has no room for: it
// compiles to 71 arithmetic slots against ps_2_0's 64. Hoisting the shared terms did not help
// because the compiler had already found them, so the choice was a cheaper mask, a cheaper
// scanline, or this. This costs nothing and looks better than the cross it replaces.
float3 Glow(float2 uv)
{
    float3 spill = SpriteTexture.Sample(SpriteTextureSampler, uv + GlowStep).rgb;
    spill += SpriteTexture.Sample(SpriteTextureSampler, uv - GlowStep).rgb;

    float2 across = float2(GlowStep.x, -GlowStep.y);

    spill += SpriteTexture.Sample(SpriteTextureSampler, uv + across).rgb;
    spill += SpriteTexture.Sample(SpriteTextureSampler, uv - across).rgb;

    return spill * 0.25;
}

// ---------------------------------------------------------------------------------------------
// Scanlines alone.
// ---------------------------------------------------------------------------------------------

float4 ScanlinesOnly(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 picture = SpriteTexture.Sample(SpriteTextureSampler, texCoord);
    float2 outputPixel = texCoord * BackBufferSize;

    return float4(picture.rgb * Gain * Scanline(outputPixel.y), picture.a);
}

// ---------------------------------------------------------------------------------------------
// Scanlines, the grille and the glow. Flat, so the picture still lines up with the window.
// ---------------------------------------------------------------------------------------------

float4 ScanlinesAndPhosphor(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 picture = SpriteTexture.Sample(SpriteTextureSampler, texCoord);
    float2 outputPixel = texCoord * BackBufferSize;

    float3 lit = picture.rgb + Glow(texCoord) * GlowAmount;

    lit *= Gain * Scanline(outputPixel.y);
    lit *= Grille(outputPixel.x);

    return float4(lit, picture.a);
}

// ---------------------------------------------------------------------------------------------
// The whole tube: the picture bowed inside its own frame, with the corners falling away.
// ---------------------------------------------------------------------------------------------

float4 WholeTube(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : SV_Target
{
    // Where this pixel is within the composition, from -1 to 1 either way, so the bow is about
    // the middle of the picture and not the middle of the window.
    float2 fromCentre = ((texCoord - CompositionRect.xy) / CompositionRect.zw) * 2.0 - 1.0;

    // How far out this pixel is, squared. Held in a local because the bow and the dimming of the
    // corners are the same measurement asked twice, and ps_2_0 has 64 arithmetic slots to spend.
    float fallOff = dot(fromCentre, fromCentre);

    // Pushed outward in proportion to the square of the distance, which is what a sphere seen
    // flat does. The picture is read from further in at the edges, so it appears to bow out.
    float2 bowed = fromCentre * (1.0 + Curvature * fallOff);

    // Past the edge of the tube there is no picture, only the inside of the bezel. step products
    // rather than a pair of ifs, because ps_2_0 has no branches to take.
    float2 onTube = step(abs(bowed), float2(1.0, 1.0));

    float2 uv = CompositionRect.xy + (bowed * 0.5 + 0.5) * CompositionRect.zw;
    float2 outputPixel = texCoord * BackBufferSize;

    float3 picture = SpriteTexture.Sample(SpriteTextureSampler, uv).rgb + Glow(uv) * GlowAmount;

    // Gathered into one multiply rather than four. The corners of a tube are further from the gun
    // and never quite as bright, and past its edge there is no picture at all.
    picture *= Gain * Scanline(outputPixel.y)
        * Grille(outputPixel.x)
        * (1.0 - VignetteAmount * fallOff)
        * onTube.x * onTube.y;

    return float4(picture, 1.0);
}

technique Scanlines
{
    pass Lines
    {
        PixelShader = compile ps_6_0 ScanlinesOnly();
    }
}

technique Phosphor
{
    pass LinesAndGrille
    {
        PixelShader = compile ps_6_0 ScanlinesAndPhosphor();
    }
}

technique Tube
{
    pass TheWholeThing
    {
        PixelShader = compile ps_6_0 WholeTube();
    }
}
