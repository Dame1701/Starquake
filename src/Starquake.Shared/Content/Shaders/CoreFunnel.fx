// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
// The planet's core: a funnel of energy turning about the elements that sit in it.
//
// Drawn over a plain filled rectangle rather than over any artwork - Shapes is a SpriteBatch with
// a one by one white texture, so the coordinates run nought to one across whatever rectangle it is
// stretched over and every pixel here is worked out rather than sampled. That is why this shader
// reads no texture at all, and why it needs no sprite rectangle: there is no sheet to stray into.

// Turns with the logic clock, so the funnel spins at the same rate whatever the display does.
float Turned;

// How many arms the spiral has, and how hard they wind inward. Winding is what makes it a funnel
// rather than a catherine wheel: the arms have to sweep toward the throat, not stand straight out
// of it.
float Arms;
float Twist;

// Where the funnel fades out, as a fraction of the rectangle - and the reciprocals of the three
// spans measured from it. They are worked out on the processor because they are constants: a
// divide costs slots here and this pass has none to spare, where the same division done once a
// frame on a value that never changes costs nothing worth counting.
float OuterEdge;
float OverOuterEdge;
float OverThroatEdge;
float OverSpan;

// The two colours of it: the throat and the arms further out.
float3 ThroatColour;
float3 ArmColour;

// How brightly the whole thing burns, and how much of that the throat gets - it is one bright
// patch where the arms are thin lines spread over the room, so the two are not the same number.
float Brightness;
float ThroatBrightness;

float4 Funnel(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : SV_Target
{
    float2 fromCentre = texCoord - 0.5;

    float distance = length(fromCentre);
    float angle = atan2(fromCentre.y, fromCentre.x);

    // The arms. Winding them by the log of the distance rather than the distance itself is what
    // makes the spiral keep its shape as it goes in - a plain radial twist bunches the arms into
    // a blur at the middle, which is exactly where the eye is meant to be able to follow them.
    // Wound by the reciprocal of the distance rather than by the distance itself, so the arms
    // sweep harder as they near the middle instead of bunching into a blur there - which is where
    // the eye is most meant to be able to follow them. A logarithm is the textbook spiral and
    // costs more slots than this pass has.
    float wound = angle * Arms + Twist / max(distance, 0.05) + Turned;

    // Cubed so the arms are bright lines with dark between them rather than an even wash of light
    // and shade. Written as multiplies rather than as pow, which compiles to a logarithm, a
    // multiply and an exponent - three of those put this pass over ps_2_0's sixty-four slots.
    float band = saturate(sin(wound) * 0.5 + 0.5);
    float arm = band * band * band;

    // Gone by the outer edge, and falling away into the throat as well, so the middle is a hole
    // the elements sit in rather than a blob of light on top of them.
    float across = saturate((OuterEdge - distance) * OverSpan);
    float reach = across * saturate(distance * OverThroatEdge);

    // The throat glows whatever the arms are doing, which is what gives the funnel a bottom to
    // look down rather than an empty hole.
    float depth = saturate(1.0 - distance * OverOuterEdge);
    float squared = depth * depth;
    float throat = squared * squared;

    float3 lit = ArmColour * (arm * reach * Brightness)
        + ThroatColour * (throat * ThroatBrightness);

    // Additive, so the alpha carries the light rather than cutting a shape out of what is behind.
    // The alpha is taken down with the light rather than left at full, so the funnel is something
    // the core is seen through rather than something drawn over it.
    return float4(lit, saturate(arm * reach * Brightness + throat * ThroatBrightness)) * tint;
}

technique Funnel
{
    pass Turn
    {
        PixelShader = compile ps_6_0 Funnel();
    }
}
