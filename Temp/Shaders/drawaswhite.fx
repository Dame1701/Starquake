sampler2D baseMap;

struct PS_INPUT 
{
   float2 Texcoord : TEXCOORD0;

};

float4 main( PS_INPUT Input ) : COLOR0
{
   float4 color = tex2D( baseMap, Input.Texcoord );
   return float4(0.5f, 0.5f, 0.5f, color.b);
}

technique DrawAsWhite
{
    pass Pass1
    {
        PixelShader = compile ps_4_0 main();
    }
}