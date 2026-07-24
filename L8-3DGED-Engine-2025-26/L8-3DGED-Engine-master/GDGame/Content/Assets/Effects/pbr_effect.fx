#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

#define MAX_LIGHTS 8

// ===== MATRICES =====
float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 WorldInverseTranspose;

// ===== TEXTURES =====
texture AlbedoTexture;
sampler2D AlbedoSampler = sampler_state
{
    Texture = <AlbedoTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

texture NormalTexture;
sampler2D NormalSampler = sampler_state
{
    Texture = <NormalTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

texture SRMTexture; // R=Specular, G=Roughness, B=Metallic
sampler2D SRMSampler = sampler_state
{
    Texture = <SRMTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

texture EmissiveTexture;
sampler2D EmissiveSampler = sampler_state
{
    Texture = <EmissiveTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

// ===== MATERIAL PROPERTIES =====
float4 AlbedoColor = float4(1, 1, 1, 1);
float3 EmissiveColor = float3(0, 0, 0);
float EmissiveStrength = 1.0;
float GlobalEmissiveMultiplier = 1.0;

// Default material values if no SRM texture
float DefaultSpecular = 0.5;
float DefaultRoughness = 0.5;
float DefaultMetallic = 0.0;

// ===== LIGHTING =====
float3 CameraPosition;
float3 AmbientColor = float3(0.1, 0.1, 0.1);

// Point Lights
int ActiveLightCount = 0;
float3 LightPositions[MAX_LIGHTS];
float3 LightColors[MAX_LIGHTS];
float LightRanges[MAX_LIGHTS];
float LightIntensities[MAX_LIGHTS];

// ===== TEXTURE FLAGS =====
bool UseAlbedoTexture = true;
bool UseNormalTexture = false;
bool UseSRMTexture = false;
bool UseEmissiveTexture = false;

// ===== VERTEX SHADER INPUT =====
struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TexCoord : TEXCOORD0;
    float3 Tangent : TANGENT0;
    float3 Binormal : BINORMAL0;
};

// ===== VERTEX SHADER OUTPUT / PIXEL SHADER INPUT =====
struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float4 WorldPosAndDepth : TEXCOORD1; // xyz = WorldPos, w = unused
    float3 Normal : TEXCOORD2;
    float3 Tangent : TEXCOORD3;
    float3 Binormal : TEXCOORD4;
};

// ===== CONSTANTS =====
static const float PI = 3.14159265359;
static const float EPSILON = 0.00001;

// ===== UTILITY FUNCTIONS =====

// Unpack normal from normal map
float3 UnpackNormal(float4 packedNormal)
{
    float3 normal;
    normal.xy = packedNormal.xy * 2.0 - 1.0;
    normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
    return normal;
}

// Transform normal from tangent space to world space
float3 TangentToWorld(float3 tangentNormal, float3 worldNormal, float3 worldTangent, float3 worldBinormal)
{
    float3x3 TBN = float3x3(worldTangent, worldBinormal, worldNormal);
    return normalize(mul(tangentNormal, TBN));
}

// ===== PBR FUNCTIONS =====

// Normal Distribution Function (GGX/Trowbridge-Reitz)
float DistributionGGX(float3 N, float3 H, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;
    
    float num = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;
    
    return num / max(denom, EPSILON);
}

// Geometry Function (Smith's method with Schlick-GGX)
float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r * r) / 8.0;
    
    float num = NdotV;
    float denom = NdotV * (1.0 - k) + k;
    
    return num / max(denom, EPSILON);
}

float GeometrySmith(float3 N, float3 V, float3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
    float ggx1 = GeometrySchlickGGX(NdotL, roughness);
    
    return ggx1 * ggx2;
}

// Fresnel Equation (Schlick's approximation)
float3 FresnelSchlick(float cosTheta, float3 F0)
{
    return F0 + (1.0 - F0) * pow(saturate(1.0 - cosTheta), 5.0);
}

// ===== VERTEX SHADER =====
VertexShaderOutput MainVS(VertexShaderInput input)
{
    VertexShaderOutput output;
    
    // Transform position
    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);
    
    // Pass through texture coordinates
    output.TexCoord = input.TexCoord;
    
    // Transform normals, tangents, binormals to world space
    output.WorldPosAndDepth = float4(worldPosition.xyz, 0.0);
    output.Normal = normalize(mul(input.Normal, (float3x3) WorldInverseTranspose));
    output.Tangent = normalize(mul(input.Tangent, (float3x3) World));
    output.Binormal = normalize(mul(input.Binormal, (float3x3) World));
    
    return output;
}

// ===== PIXEL SHADER =====
float4 MainPS(VertexShaderOutput input) : COLOR0
{
    // Extract world position
    float3 worldPos = input.WorldPosAndDepth.xyz;
    
    // Sample textures
    float4 albedoSample = UseAlbedoTexture ? tex2D(AlbedoSampler, input.TexCoord) : float4(1, 1, 1, 1);
    float3 albedo = albedoSample.rgb * AlbedoColor.rgb;
    float alpha = albedoSample.a * AlbedoColor.a;
    
    // Get normal
    float3 N = normalize(input.Normal);
    if (UseNormalTexture)
    {
        float3 tangentNormal = UnpackNormal(tex2D(NormalSampler, input.TexCoord));
        N = TangentToWorld(tangentNormal, input.Normal, input.Tangent, input.Binormal);
    }
    
    // Sample SRM (Specular, Roughness, Metallic)
    float3 srmSample = UseSRMTexture ? tex2D(SRMSampler, input.TexCoord).rgb : float3(DefaultSpecular, DefaultRoughness, DefaultMetallic);
    float specular = srmSample.r;
    float roughness = max(srmSample.g, 0.04); // Clamp to avoid division by zero
    float metallic = srmSample.b;
    
    // Calculate view direction
    float3 V = normalize(CameraPosition - worldPos);
    
    // Calculate F0 (surface reflection at zero incidence)
    float3 F0 = float3(0.04, 0.04, 0.04); // Dielectric base reflectivity
    F0 = lerp(F0, albedo, metallic);
    
    // Initialize lighting accumulator
    float3 Lo = float3(0.0, 0.0, 0.0);
    
    // Process each active light
    for (int i = 0; i < ActiveLightCount; i++)
    {
        // Calculate light direction and distance
        float3 L = LightPositions[i] - worldPos;
        float distance = length(L);
        L = normalize(L);
        
        // Attenuation
        float attenuation = 1.0 / (distance * distance);
        float range = LightRanges[i];
        float rangeAttenuation = saturate(1.0 - (distance / range));
        rangeAttenuation *= rangeAttenuation;
        attenuation *= rangeAttenuation;
        
        float3 radiance = LightColors[i] * LightIntensities[i] * attenuation;
        
        // Calculate half vector
        float3 H = normalize(V + L);
        
        // Cook-Torrance BRDF
        float NDF = DistributionGGX(N, H, roughness);
        float G = GeometrySmith(N, V, L, roughness);
        float3 F = FresnelSchlick(max(dot(H, V), 0.0), F0);
        
        float3 numerator = NDF * G * F;
        float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0);
        float3 specularBRDF = numerator / max(denominator, EPSILON);
        
        // Energy conservation
        float3 kS = F; // Specular contribution
        float3 kD = float3(1.0, 1.0, 1.0) - kS; // Diffuse contribution
        kD *= 1.0 - metallic; // Metallic surfaces have no diffuse
        
        // Lambertian diffuse
        float3 diffuse = kD * albedo / PI;
        
        // Add to outgoing radiance
        float NdotL = max(dot(N, L), 0.0);
        Lo += (diffuse + specularBRDF) * radiance * NdotL;
    }
    
    // Ambient lighting (very simple)
    float3 ambient = AmbientColor * albedo;
    
    // Sample emissive
    float3 emissive = float3(0, 0, 0);
    if (UseEmissiveTexture)
    {
        float4 emissiveSample = tex2D(EmissiveSampler, input.TexCoord);
        emissive = emissiveSample.rgb * EmissiveColor * emissiveSample.a * EmissiveStrength * GlobalEmissiveMultiplier;
    }
    else
    {
        emissive = EmissiveColor * EmissiveStrength * GlobalEmissiveMultiplier;
    }
    
    // Final color
    float3 color = ambient + Lo + emissive;
    
    // Simple tone mapping (Reinhard)
    color = color / (color + float3(1.0, 1.0, 1.0));
    
    // Gamma correction (saturate ensures non-negative values for pow)
    color = pow(saturate(color), float3(1.0 / 2.2, 1.0 / 2.2, 1.0 / 2.2));
    
    return float4(color, alpha);
}

// ===== TECHNIQUE =====
technique PBR
{
    pass P0
    {
        VertexShader = compile vs_4_0 MainVS();
        PixelShader = compile ps_4_0 MainPS();
    }
}
