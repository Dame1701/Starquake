// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
// Drains the colour and the light out of whatever SpriteBatch is drawing.
//
// Saturation is 1 for the picture untouched and 0 for black and white. Brightness is 1 for the
// picture untouched and 0 for black. Together they take a lit, coloured screen down to a dark
// grey one and back.
//
// The colour half cannot be done with a tint, which is why this exists: SpriteBatch multiplies
// each channel on its own, and turning a colour grey needs the three channels added together,
// which no multiplier can do. The darkening half could be a tint, but it belongs with the
// draining rather than in a second pass over the same pixels.

Texture2D SpriteTexture : register(t0);
SamplerState SpriteTextureSampler : register(s0);

float Saturation;
float Brightness;

float4 DrainTheColour(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : SV_Target
{
    float4 texel = SpriteTexture.Sample(SpriteTextureSampler, texCoord) * tint;

    // Rec. 601 luma. The green weight is the large one because the eye reads most of a
    // picture's brightness out of green.
    float grey = dot(texel.rgb, float3(0.299, 0.587, 0.114));

    float3 drained = lerp(float3(grey, grey, grey), texel.rgb, Saturation);

    // The alpha is carried through untouched, and only the colour is dimmed. Everything drawn
    // here is premultiplied, so scaling the three colour channels and leaving alpha alone is
    // what darkening means - the same thing a tint would do.
    return float4(drained * Brightness, texel.a);
}

technique Desaturate
{
    pass DrainIt
    {
        PixelShader = compile ps_6_0 DrainTheColour();
    
    }
}
