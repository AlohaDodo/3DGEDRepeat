#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Practical game UI effect parameters go here...

// Texture (SpriteBatch provides this automatically)
sampler s0;

// Vertex shader input
struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

// Vertex shader output / Pixel shader input
struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

// Helper: Convert color to grayscale
float GetLuminance(float3 color)
{
    // Standard luminance calculation
    return dot(color, float3(0.299, 0.587, 0.114));
}

// Pixel Shader
float4 MainPS(VertexShaderOutput input) : COLOR0
{
    // Sample the texture
    float4 texColor = tex2D(s0, input.TexCoord);
    
    // Apply vertex color (SpriteBatch tint)
    float4 finalColor = texColor * input.Color;
    
    // Re-premultiply alpha
    return finalColor;
}

// Technique for SpriteBatch
technique SpriteBatch
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};

