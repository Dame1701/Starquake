// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
// A part going into the planet's core. The room is wrung round the slot it went into and springs
// back, as though the core had taken the energy and passed it through the room.
//
// A sibling of TeleportArrival.fx and a deliberately different shape: that one is standing waves
// settling, because arriving somewhere is a thing that happens *to* the room. This is a twist
// about a point, because something has been put *into* it and the room is answering.

Texture2D SpriteTexture : register(t0);
SamplerState SpriteTextureSampler : register(s0);

// One as the part lands and nought once the room has recovered.
float Strength;

// Where it went in, in texture coordinates, and the reciprocal of how far the wring reaches.
float2 Centre;
float OverReach;

// How far round the room is wrung at the centre, in radians, and how many times the ripple
// crosses the distance it reaches over.
float Twist;
float Ripples;

// Where the ripple has travelled to. From the logic clock, so it runs at the same rate whatever
// the display is doing.
float Phase;

// The light the core throws back out through the room as it takes the part.
float3 SurgeColour;
float Surge;

float4 Wring(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : SV_Target
{
    float2 fromSlot = texCoord - Centre;

    // Nought at the slot and one at the edge of the reach, so everything below falls away with
    // distance rather than wringing the far corners of the room as hard as the middle.
    // 'out' is a reserved word here, as 'triangle' was in Crt.fx.
    float outward = saturate(length(fromSlot) * OverReach);
    float near = 1.0 - outward;

    // Hardest at the slot and gone by the reach, with a wave running outward through it - the
    // wring is not an even turn but a pulse travelling out of the core.
    float turned = Twist * Strength * near * near * sin(outward * Ripples - Phase);

    // Rotating the offset about the slot. The two trig calls are the cost of this shader and the
    // reason there is nothing else in it.
    float wringSin = sin(turned);
    float wringCos = cos(turned);

    float2 wrung = float2(
        fromSlot.x * wringCos - fromSlot.y * wringSin,
        fromSlot.x * wringSin + fromSlot.y * wringCos);

    // Held inside the picture, or a tap off the edge smears the outermost row across the border.
    float4 room = SpriteTexture.Sample(SpriteTextureSampler, saturate(Centre + wrung));

    // The core's own light coming back out through the room, strongest where the part went in.
    float3 thrown = SurgeColour * (Surge * Strength * near * near);

    return float4(room.rgb + thrown, room.a) * tint;
}

technique Wring
{
    pass TakeIt
    {
        PixelShader = compile ps_6_0 Wring();
    }
}
