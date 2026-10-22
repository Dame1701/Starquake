// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
// Blob's shot, drawn as a beam and out of nothing at all.
//
// There is no artwork behind this. It is laid over a plain filled rectangle - Shapes is a
// SpriteBatch with a one by one white texture - stretched along the way the shot is going, so
// every pixel of the beam is worked out rather than sampled. The flame sprite that used to be
// drawn on foot was taken out of the game on purpose a long time ago, on the grounds that it
// added nothing, and putting it back proved the point: what the shot wanted was not a picture of
// a flame but light of its own.

// How tight the white line down the middle is, as a reciprocal, and the colours of the line and of
// what spreads off it. The three reciprocals in here are worked out on the processor because they
// are constants between frames: a divide costs slots and this pass has none - see BUILD.md, which
// has the two ways of buying them back and this is one of them.
float OverCoreWidth;
float3 CoreColour;
float3 EdgeColour;

// How far the coloured spread reaches, and how hard the whole thing burns. Intensity is the one
// knob for "too much".
float Spread;
float Intensity;

// How far the beam snakes either side of true, how many waves there are along it, and where in
// their travel they are. The phase comes off the logic clock, so the beam writhes at the same
// rate whatever the display is doing.
float WaveAmplitude;
float WaveFrequency;
float WavePhase;

// How far along the beam it fades in, as a reciprocal. A beam that stopped dead at its back end
// would read as a bar of light with a cut edge.
float OverEndFade;

// The balls of plasma at either end: how big they are as a reciprocal, how much they are stretched
// along the beam so that they are heads and not beads, how hard they burn, and how much of that
// the trailing one gets. The shot is at the front, so the back is its wake and burns less.
float OverHeadSize;
float HeadStretch;
float HeadBoost;
float BackShare;

float4 Beam(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : SV_Target
{
    // The quad runs nought to one along the shot and across it, so nothing is sampled and there is
    // no artwork to be in step with - this is drawn over a plain filled rectangle.
    float along = texCoord.x;

    // Where the middle of the beam is at this point along it. Two waves at rates that do not
    // divide one another, so it writhes rather than rippling evenly - one sine alone reads as a
    // skipping rope.
    float snake = (sin(along * WaveFrequency + WavePhase)
        + 0.5 * sin(along * WaveFrequency * 2.3 - WavePhase * 1.7)) * WaveAmplitude;

    float fromAxis = saturate(abs(texCoord.y - 0.5 + snake) * 2.0);

    // The hot line. Squared so it is a line rather than a band.
    float core = saturate(1.0 - fromAxis * OverCoreWidth);
    core *= core;

    // And the colour spreading off it, reaching further but falling away.
    float off = saturate(1.0 - fromAxis * Spread);
    off *= off;

    // Taken out at the back, so the beam trails away rather than ending in a cut edge. The front
    // is left alone: that is where the shot actually is, and it has the head on it.
    float tail = saturate(along * OverEndFade);

    // A ball at each end, from one measurement: the distance to whichever end is nearer is the
    // same sum for both, so the second costs nothing over the first - which matters, because this
    // pass has no slots to spare. Squashed along the beam so they are balls drawn out into the
    // shot rather than beads stuck on its ends.
    float toNearerEnd = min(along, 1.0 - along);

    float2 toBall = float2(toNearerEnd * HeadStretch, (texCoord.y - 0.5) * 2.0);
    float ball = saturate(1.0 - length(toBall) * OverHeadSize);
    ball *= ball;

    // The front one burns full and the back one less, because the shot is at the front and the
    // back is only its wake. step rather than a branch: ps_2_0 has none.
    ball *= lerp(BackShare, 1.0, step(0.5, along));

    float3 lit = CoreColour * (core * tail + ball * HeadBoost)
        + EdgeColour * (off * tail + ball);

    return float4(lit * Intensity, saturate(core * tail + off * tail + ball) * Intensity) * tint;
}

technique Beam
{
    pass Fire
    {
        PixelShader = compile ps_6_0 Beam();
    }
}
