// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
// Drains the colour out of whatever SpriteBatch is drawing, leaving the brightness alone.
//
// Saturation is 1 for the picture untouched and 0 for black and white; anything between is a
// mix of the two. It cannot be done with a tint, which is why this exists: SpriteBatch
// multiplies each channel on its own, and turning a colour grey needs the three channels
// added together, which no multiplier can do.
//
// ps_2_0 because Content.mgcb is built /profile:Reach, which allows no higher. There is room
// to spare - this is a dot product and a lerp.

sampler2D SpriteTexture : register(s0);

float Saturation;

float4 DrainTheColour(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float4 texel = tex2D(SpriteTexture, texCoord) * tint;

    // Rec. 601 luma. The green weight is the large one because the eye reads most of a
    // picture's brightness out of green.
    float brightness = dot(texel.rgb, float3(0.299, 0.587, 0.114));

    // The alpha is carried through untouched. Everything drawn here is premultiplied, and
    // because this mixes two colours that were both premultiplied by the same alpha, the
    // result is premultiplied by it too.
    return float4(lerp(float3(brightness, brightness, brightness), texel.rgb, Saturation), texel.a);
}

technique Desaturate
{
    pass DrainIt
    {
        PixelShader = compile ps_2_0 DrainTheColour();
    }
}
