// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
// Blob's energy failing him: drained by a nasty, and then run out altogether - the alarm while the
// charge builds, the cracking, and the discharge he comes apart in.
//
// Blob's artwork is greyscale - every texel has red, green and blue equal, which is why the game
// has always tinted him rather than drawn him in colour. That is what makes a heat ramp possible
// here: his own brightness can be mapped onto any colours at all without an existing hue to fight,
// so he can run from ember through orange to white-hot as the charge climbs. The alarm used to be
// a flat red tint, which is the same idea with one stop instead of three.
//
// ps_2_0, because Content.mgcb is built /profile:Reach.

sampler2D SpriteTexture : register(s0);

// Blob's own square of his strip. He is eleven frames side by side, so a tap that wanders out of
// this lands on a different pose of him.
float4 SpriteRect;

// How far the overload has got, nought as the alarm starts and one as he goes.
float Charge;

// This frame's brightness, a little above or below one, from the logic clock.
float Flicker;

// The three stops of the heat ramp, coldest first.
float3 EmberColour;
float3 MidColour;
float3 HotColour;

// The skin of charge crawling over him: what it burns, and how much of it there is.
float3 RimColour;
float RimAmount;

// How far gone his energy is, nought when it is full and one when a nasty has it all, and what
// the loss of it burns.
float Drain;
float3 DrainColour;

// How far out the rim is looked for, in texture coordinates.
float2 RimStep;

// How much of the inside of a discharge shell is left showing through its front.
float InteriorWash;

// How far open the cracks are, nought to one, and what comes out of them.
float Split;
float3 CrackColour;
float CrackBrightness;

// How wide a crack is once fully open, and how many of them run across him.
float CrackWidth;
float CrackDensity;

float2 OnThisFrame(float2 uv)
{
    return clamp(uv, SpriteRect.xy, SpriteRect.xy + SpriteRect.zw);
}

// How much of an edge this texel is. Blob is better than half solid, so his outline is where his
// own alpha stands above his neighbours' - no silhouette has to be stored, and it costs four taps.
float Rim(float2 uv, float here)
{
    float around = tex2D(SpriteTexture, OnThisFrame(uv + RimStep)).a;
    around += tex2D(SpriteTexture, OnThisFrame(uv - RimStep)).a;

    float2 across = float2(RimStep.x, -RimStep.y);

    around += tex2D(SpriteTexture, OnThisFrame(uv + across)).a;
    around += tex2D(SpriteTexture, OnThisFrame(uv - across)).a;

    return saturate(here - around * 0.25);
}

// ---------------------------------------------------------------------------------------------
// The charge building in him, while the alarm sounds and while he swells.
// ---------------------------------------------------------------------------------------------

float4 Charging(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float4 blob = tex2D(SpriteTexture, texCoord);

    // Greyscale artwork, so any channel is his brightness. Divided back out of the premultiplied
    // alpha, or his soft edges would read as dark and never take any heat.
    float lit = blob.r / max(blob.a, 0.001);

    // The ramp slides hotter as the charge climbs: at nought only his brightest parts are off the
    // bottom of it, and by one even his shadows are glowing.
    float heat = saturate(lit * (1.0 + Charge * 2.0) + Charge * 0.25);

    float3 burning = heat < 0.5
        ? lerp(EmberColour, MidColour, heat * 2.0)
        : lerp(MidColour, HotColour, heat * 2.0 - 1.0);

    // Scaled by how bright he was there, so his shape survives the ramp instead of becoming a
    // silhouette filled with one colour.
    float3 body = burning * lit;

    float skin = Rim(texCoord, blob.a) * RimAmount * (0.35 + Charge) * Flicker;

    return float4((body + RimColour * skin) * blob.a, blob.a) * tint;
}

// ---------------------------------------------------------------------------------------------
// Energy being pulled out of him by a nasty. The red used to be a flat tint - full red, with green
// and blue taken down together - which reads as a healthy light of a different colour. Energy
// going out of something is not steady, so it is unsteady here: the wash moves with the flicker,
// and a corona gathers on his outline where it is leaving him.
// ---------------------------------------------------------------------------------------------

float4 Draining(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float4 blob = tex2D(SpriteTexture, texCoord);

    // Greyscale artwork, so any channel is his brightness. Divided back out of the premultiplied
    // alpha, or his soft edges would read as dark and take no colour.
    float lit = blob.r / max(blob.a, 0.001);

    // The flicker bites harder the more of him has gone, so a glancing touch barely wavers and a
    // sustained one has him guttering.
    float draining = saturate(Drain * (1.0 + (Flicker - 1.0) * 2.0));

    // His own grey toward the drain colour, scaled by how bright he was there, so his shape
    // survives instead of becoming a silhouette filled with one colour.
    float3 body = lerp(float3(lit, lit, lit), DrainColour * lit, draining);

    // Gathered on the outline, which is where it is leaving him.
    float corona = Rim(texCoord, blob.a) * RimAmount * draining;

    return float4((body + DrainColour * corona) * blob.a, blob.a) * tint;
}

// ---------------------------------------------------------------------------------------------
// Held open and splitting, the beat before the burst.
// ---------------------------------------------------------------------------------------------

float4 Splitting(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float4 blob = tex2D(SpriteTexture, texCoord);

    float lit = blob.r / max(blob.a, 0.001);
    float2 local = (texCoord - SpriteRect.xy) / SpriteRect.zw;

    // The *contours* of a wandering field are what a crack looks like - a line that curves and
    // forks rather than one that runs straight. One sine across a slope wanders enough; two of
    // them was better and costs about ten of ps_2_0's sixty-four slots, which this pass has not
    // got. His own brightness is folded in as well, so the cracks follow the shape he is drawn
    // with rather than ignoring it.
    float field = sin(local.x * 9.0 + local.y * 5.0) + (local.x - local.y) * 2.5 + lit * 1.5;

    // frac takes every contour rather than one, so he splits in several places at once.
    float contour = abs(frac(field * CrackDensity) - 0.5) * 2.0;

    // They open from hairline to their full width as the split runs.
    float crack = 1.0 - saturate(contour / max(CrackWidth * Split, 0.001));

    // He is already as hot as the charge gets by the time he is splitting, so the ramp is at its
    // top rather than climbing - what moves now is the light coming out of him.
    float3 body = lerp(MidColour, HotColour, saturate(lit * 1.6)) * lit;

    // No rim here, unlike the charging pass. Partly the budget, and partly that blue arcing on his
    // outline competes with light pouring out of his middle - by this point the cracks are the
    // thing to look at.
    float3 burning = body * (1.0 - crack)
        + CrackColour * (crack * CrackBrightness * Flicker);

    return float4(burning * blob.a, blob.a) * tint;
}

// ---------------------------------------------------------------------------------------------
// The discharge. Each copy of him in the flare is drawn as a shell rather than a ghost: the
// outline burns and the inside of it is knocked right back, so five of them read as five
// expanding fronts instead of five Blobs on top of one another. The colour of each comes in on
// the tint, which is per-draw and so costs no second batch.
// ---------------------------------------------------------------------------------------------

float4 Discharging(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float4 blob = tex2D(SpriteTexture, texCoord);

    float lit = blob.r / max(blob.a, 0.001);
    float skin = Rim(texCoord, blob.a);

    // The shell is the outline, and what is inside it is left as a faint wash of his own
    // brightness so the shape is still readable through the front.
    float3 shell = (skin * RimAmount * Flicker + lit * InteriorWash) * blob.a;

    return float4(shell, blob.a * saturate(skin * RimAmount + lit * InteriorWash)) * tint;
}

technique Charging
{
    pass BuildUp
    {
        PixelShader = compile ps_2_0 Charging();
    }
}

technique Draining
{
    pass LoseIt
    {
        PixelShader = compile ps_2_0 Draining();
    }
}

technique Splitting
{
    pass OpenUp
    {
        PixelShader = compile ps_2_0 Splitting();
    }
}

technique Discharging
{
    pass ComeApart
    {
        PixelShader = compile ps_2_0 Discharging();
    }
}
