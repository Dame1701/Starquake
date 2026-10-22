// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
// The flight computer's display failing. The page is readable, with a line of interference
// wandering through it, and every second or two it breaks up badly for a third of a second: bands
// of it thrown sideways, the colour channels pulled apart, a few lines losing their signal
// altogether, and now and again the vertical hold letting go so the whole picture slides.
//
// It reads the finished page out of a render target rather than drawing over it, which is the
// whole of why the text breaks up along with the artwork instead of swimming about on top of it.
// The report is one picture by the time this runs, and a display that is failing fails at the
// glass rather than at one of the layers behind it.
//
// Deliberately not a tube. Shaders/Crt.fx already scans, bows and glows the whole composition
// this is composed into, so there is not a scanline here: this is the signal going, not the glass.

Texture2D PageTexture : register(t0);
SamplerState PageTextureSampler : register(s0);

// How badly the computer is failing this frame, nought to one. Everything below is scaled by it,
// so at nought this shader is the page untouched.
float Breakup;

// What the bands are scrambled by, so they are never in the same place twice. Taken from the
// logic clock, so the breakup runs at the same rate whatever the display is doing.
float Phase;

// How far the picture has slid down this frame, as a fraction of its height, with whatever runs
// off the bottom coming back round at the top. Nought on most frames - the hold only goes at the
// height of some of the bursts. Worked out on the processor, because it is one number for the
// whole page rather than a number per pixel - see BUILD.md.
float Roll;

// How many bands the page is cut into, and how closely the fine interference lines are spaced,
// both as a count down the page.
float2 BandScale;

// How far a band is thrown sideways at the worst, as a fraction of the width, and how far the red
// and blue are pulled either side of it.
float Slip;
float ChromaSplit;

// What proportion of the bands lose their signal at the worst, and the cold grey one that has
// lost it is left showing.
float Dropout;
float3 SignalColour;

float4 Failing(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : SV_Target
{
    // The hold going. Only where the picture is read from moves, so the bands below are measured
    // against the glass and stay put while the picture slides through them - which is what they do
    // on the real thing, the fault being in the signal and the interference being in the set.
    float2 uv = float2(texCoord.x, texCoord.y + Roll);

    // One value for a whole band and a different one for the next, with the grid of bands itself
    // travelling, so nothing settles into fixed strips.
    float band = floor(texCoord.y * BandScale.x + Phase);
    float noise = frac(sin(band * 12.9898 + Phase) * 43758.5453);

    // How far this band is thrown. Cubed, so most of them barely move and the few that do are
    // thrown right across: a page where every band slides by its own amount reads as a shear
    // rather than as a breakup.
    float thrown = noise * 2.0 - 1.0;
    thrown = thrown * thrown * thrown;

    // And a one-line hash over the top of that, which is what the eye reads as interference rather
    // than as the picture having been cut up and laid back down.
    float hash = frac(sin(texCoord.y * BandScale.y + Phase * 1.7) * 7919.0) - 0.5;

    uv.x += (thrown + hash * 0.25) * Slip * Breakup;

    // Wrapped rather than clamped, on both axes: a band sliding off one edge belongs back on the
    // other, where clamping would instead smear the outermost column or row of the page across the
    // border and read as the picture leaking.
    uv = frac(uv);

    // The colour channels pulled apart. Three taps of the page and not one, which is the cost of
    // the fringing - a tint cannot do it, because the channels have to come from different places.
    float split = ChromaSplit * Breakup;

    float4 page = PageTexture.Sample(PageTextureSampler, uv);
    float red = PageTexture.Sample(PageTextureSampler, frac(uv + float2(split, 0.0))).r;
    float blue = PageTexture.Sample(PageTextureSampler, frac(uv - float2(split, 0.0))).b;

    float3 lit = float3(red, page.g, blue);

    // A band that has lost its signal altogether, left as the cold grey a set keeps when nothing
    // is driving it. step rather than a branch, because ps_2_0 has none.
    float lost = step(1.0 - Dropout * Breakup, noise);

    // Scaled by what the page has there, so a dead band over an empty corner stays empty instead
    // of the grey being painted onto nothing - everything the game composes is premultiplied.
    return float4(lerp(lit, SignalColour * page.a, lost), page.a) * tint;
}

technique Failing
{
    pass BreakUp
    {
        PixelShader = compile ps_6_0 Failing();
    }
}
