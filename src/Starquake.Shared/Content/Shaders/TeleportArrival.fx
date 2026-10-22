// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
// Arriving somewhere by teleport. The picture comes in unsettled - standing waves running through
// it, as though the screen had not finished being assembled - and settles as they die away.
//
// It displaces where the picture is read from rather than drawing anything over it, which is what
// makes it a shimmer rather than a veil: the room itself is what is moving, and when the waves are
// gone the picture is exactly the one that was there all along.

Texture2D SpriteTexture : register(t0);
SamplerState SpriteTextureSampler : register(s0);

// One as Blob arrives and nought once the room has settled. Everything below is scaled by it, so
// at nought this shader is the picture untouched.
float Strength;

// Where in their travel the waves are. Taken from the logic clock, so they run at the same rate
// whatever the display is doing.
float Phase;

// How far the picture is pushed about, as a fraction of it, and how closely the waves are spaced.
float2 Amplitude;
float2 Frequency;

// How much the brightness swims with the waves, and the cold light the arrival leaves on it.
float Glimmer;
float3 ArrivalColour;

float4 Shimmer(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : SV_Target
{
    // Each axis pushed by a wave running along the other, so the picture moves in two directions
    // at once rather than sliding bodily to one side. The two frequencies do not divide one
    // another, so the pattern does not stand still long enough to be read as stripes.
    float alongX = sin(texCoord.y * Frequency.x + Phase);
    float alongY = sin(texCoord.x * Frequency.y + Phase * 1.3);

    float2 pushed = texCoord + float2(alongX * Amplitude.x, alongY * Amplitude.y) * Strength;

    // Held inside the picture. The sampler clamps as well, but a tap off the edge would smear the
    // outermost row of pixels across the border, which reads as the room leaking.
    float4 room = SpriteTexture.Sample(SpriteTextureSampler, saturate(pushed));

    // The light swims with the waves rather than evenly, so the shimmer is in the brightness as
    // well as in the shape - a displacement alone reads as a wobble rather than as energy.
    float swim = 1.0 + alongX * alongY * Glimmer * Strength;

    // The cold of the beam, going as the room settles. Scaled by what is lit, so the dark parts of
    // the room stay dark instead of the whole picture being washed with colour.
    float3 arriving = ArrivalColour * (Strength * room.a);

    return float4(room.rgb * swim + arriving, room.a) * tint;
}

technique Shimmer
{
    pass Settle
    {
        PixelShader = compile ps_6_0 Shimmer();
    }
}
