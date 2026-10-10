// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
// The ring thrown out by a blast - Blob coming apart, and the two nasties that are machines.
//
// It reads no texture, as Halo.fx does not: the quad is drawn at the size the ring will reach and
// every pixel of it is worked out from where it falls within that square. What was there before
// was one Ring001 sprite scaled up and faded out, which is a picture of a ring growing rather than
// a blast travelling - the edge was as soft at the end as at the start, and there was only ever
// one of it.
//
// This is a wave train instead. A hot front with nothing at all ahead of it, the ripples it left
// trailing behind it, and two waves running round the rim at different rates so the edge itself
// shimmers as it travels. The front is the orange of the flames the explosion is already throwing
// and the very crest of it is nearly white, so the ring is the blast's own light.
//
// ps_2_0, because Content.mgcb is built /profile:Reach - and this one is written against that
// ceiling rather than near it. The first version of the rim waves compiled to 71 slots of the 64
// there are. What brought it back under, in order of what each was worth: a triangle wave in
// place of a cosine for the ripples, which the compiler spends ten slots on; the quad's square in
// its sheet folded into one multiply and add on the processor; one reciprocal square root serving
// both the distance and the direction; and every amplitude that could be folded into a uniform
// folded into one. See BUILD.md.

sampler2D SpriteTexture : register(s0);

// Where the quad sits in its sheet, as the scale and bias that turn a point in the sheet into one
// measured from the middle of the quad, running minus one to one across it. Two numbers rather
// than the sheet rectangle the other shaders take, because this is the same arithmetic with the
// divide already done - and the doubling that puts the edge of the quad at one is folded in with
// it, which is a slot.
float2 QuadScale;
float2 QuadBias;

// Where the front has got to, as a fraction of the quad's half-width: nought at the blast and one
// when it has reached the edge of the quad and is gone.
float Radius;

// How abruptly the front arrives. A reciprocal of a fraction of the quad, so a pixel or two of
// softness rather than a stair-stepped circle - a blast has nothing ahead of it, which is the
// whole reason this is not a sprite.
float OverFrontSoftness;

// How thick the hot front is, and how far behind it the ripples still reach, as reciprocals. Both
// measured against the radius on the processor, so the wave train stretches as the ring grows
// instead of the rings bunching up into a smear.
float OverFront;
float OverTail;

// How closely the ripples are spaced, in whole crests per unit of distance behind the front, and
// how far through a crest they have travelled. Counted in crests rather than radians because the
// wave is made with frac rather than a cosine.
float WaveRate;
float WavePhase;

// How bright the ripples are against the front. The crests below are a squared triangle and so
// peak at a quarter, which is multiplied back in here rather than in the pass - so this arrives
// as four times the share it reads as.
float TrailWeight;

// Where the two waves that pull the ring out of round have turned to - each as a cosine and a
// sine. Thirty-two undulations round the rim and sixty-four, running at different rates and in
// opposite directions, so what the eye gets is the two sliding through one another: a shimmer
// along the edge rather than a shape. Each blast starts them somewhere of its own.
//
// Both arrive already scaled by how far out of round they pull it, and the finer one by its
// share of that as well.
float2 ShimmerTurn;
float2 FineShimmerTurn;

// What the ring burns: the first everywhere - the ripples, and the outside of the front - and the
// first plus the second at the crest of the front. A lift rather than the crest's own colour,
// because a lerp costs slots this has none of to spare.
float3 EdgeColour;
float3 ColourLift;

// How hard the whole thing burns this frame, which is what carries it from struck to gone.
float Intensity;

// How quickly it is taken out at the edge of the quad, as a reciprocal, so the last of the ring
// goes rather than meeting the corner and showing the square it was drawn in.
float OverRimFade;

float4 Blast(float4 tint : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float2 fromMiddle = texCoord * QuadScale - QuadBias;

    float middleOut2 = dot(fromMiddle, fromMiddle);

    // One reciprocal square root, used twice: once with the squared distance to make the distance
    // itself, and once with the offset to make the direction. 'out' is a reserved word here, as it
    // was in CoreSurge.fx.
    float overLength = rsqrt(max(middleOut2, 0.000001));

    // Nought in the middle and one at the edge of the quad.
    float outward = middleOut2 * overLength;

    // The waves running round the rim. Worked out from the direction alone - no angle is taken,
    // because atan2 would cost more than the rest of this shader together - with each harmonic
    // got from the one below it by the double and sum angle identities.
    float2 along = fromMiddle * overLength;

    float cosTwice = along.x * along.x - along.y * along.y;
    float sinTwice = 2.0 * along.x * along.y;

    float cosFour = 2.0 * cosTwice * cosTwice - 1.0;
    float sinFour = 2.0 * sinTwice * cosTwice;

    float cosEight = 2.0 * cosFour * cosFour - 1.0;
    float sinEight = 2.0 * sinFour * cosFour;

    float cosSixteen = 2.0 * cosEight * cosEight - 1.0;
    float sinSixteen = 2.0 * sinEight * cosEight;

    float cosCoarse = 2.0 * cosSixteen * cosSixteen - 1.0;
    float sinCoarse = 2.0 * sinSixteen * cosSixteen;

    float cosFine = 2.0 * cosCoarse * cosCoarse - 1.0;
    float sinFine = 2.0 * sinCoarse * cosCoarse;

    // Each wave at where it has turned to, which is the cosine of the angle less its phase. The
    // counts went up and the amplitude down three times over: two undulations and four made an
    // oval and then a rounded square, six and eight made a flower, and sixteen and thirty-two
    // still read as a cog turning. A circle that shimmers is many small waves rather than a few
    // large ones, and the count is what buys the roundness.
    float shimmer = cosCoarse * ShimmerTurn.x + sinCoarse * ShimmerTurn.y;
    float fine = cosFine * FineShimmerTurn.x + sinFine * FineShimmerTurn.y;

    outward *= 1.0 + shimmer + fine;

    // How far behind the front this pixel is. Negative ahead of it, where there is nothing: the
    // blast has not arrived yet.
    float behind = Radius - outward;

    float arrived = saturate(behind * OverFrontSoftness);

    // The front itself. Cubed rather than raised, because pow compiles to a logarithm, a multiply
    // and an exponent - see BUILD.md.
    float front = saturate(1.0 - behind * OverFront);
    front = front * front * front;

    // The ripples trailing it, as a squared triangle wave: a crest with some body to it and the
    // troughs still dark between them, which is what makes this a train of rings rather than a
    // glowing disc. It was taken to the fourth, which drew each of them as a thread. The triangle
    // is left peaking at a half rather than at one, because scaling it back up is a slot here and
    // nothing at all in TrailWeight.
    float crest = 0.5 - abs(frac(behind * WaveRate - WavePhase) - 0.5);
    crest = crest * crest;

    // And the envelope they die away under, so they thin out with distance behind the front
    // rather than carrying on to the middle.
    float tail = saturate(1.0 - behind * OverTail);

    float glow = (front + crest * tail * tail * TrailWeight) * arrived * Intensity;

    glow *= saturate((1.0 - outward) * OverRimFade);

    float3 burning = EdgeColour + ColourLift * front;

    return float4(burning * glow, glow) * tint;
}

technique Blast
{
    pass Ring
    {
        PixelShader = compile ps_2_0 Blast();
    }
}
