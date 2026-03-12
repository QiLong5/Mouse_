#ifndef CUSTOM_URP_LIT_COMMON_INCLUDED
#define CUSTOM_URP_LIT_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// ========== Stylized Lighting Functions ==========
// 风格化光照 - 将光照分层
half StylizedDiffuse(half NdotL, half steps, half smoothness)
{
    // 将光照分成多个层级
    half steppedNdotL = floor(NdotL * steps) / steps;
    // 在层级之间平滑过渡
    return lerp(steppedNdotL, NdotL, smoothness);
}

// ========== Texture Declarations ==========
TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
TEXTURE2D(_MetallicMap);
SAMPLER(sampler_MetallicMap);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_EmissionMap);
TEXTURE2D(_OcclusionMap);
SAMPLER(sampler_OcclusionMap);

// ========== Material Properties ==========
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseColor;
    float4 _EmissionColor;
    float4 _MainTiling;
    float4 _OutlineColor;
    float4 _RimColor;
    float4 _CustomLightColor;
    float4 _CustomAmbientColor;
    float _Metallic;
    float _Smoothness;
    float _NormalScale;
    float _Cutoff;
    float _ReceiveShadows;
    float _UseNormalMap;
    float _UseEmission;
    float _EnableOutline;
    float _UseVertexColor;
    float _OutlineWidth;
    float _OcclusionStrength;
    float _RimPower;
    float _RimIntensity;
    float _ShadowSteps;
    float _ShadowSmoothness;

    // Fake Point Lights (8 lights max)
    float _FakeLight1_Enabled;
    float4 _FakeLight1_Pos;
    float4 _FakeLight1_Color;
    float _FakeLight1_Intensity;
    float _FakeLight1_Range;
    float _FakeLight1_AttenuationPower;

    float _FakeLight2_Enabled;
    float4 _FakeLight2_Pos;
    float4 _FakeLight2_Color;
    float _FakeLight2_Intensity;
    float _FakeLight2_Range;
    float _FakeLight2_AttenuationPower;

    float _FakeLight3_Enabled;
    float4 _FakeLight3_Pos;
    float4 _FakeLight3_Color;
    float _FakeLight3_Intensity;
    float _FakeLight3_Range;
    float _FakeLight3_AttenuationPower;

    float _FakeLight4_Enabled;
    float4 _FakeLight4_Pos;
    float4 _FakeLight4_Color;
    float _FakeLight4_Intensity;
    float _FakeLight4_Range;
    float _FakeLight4_AttenuationPower;

    float _FakeLight5_Enabled;
    float4 _FakeLight5_Pos;
    float4 _FakeLight5_Color;
    float _FakeLight5_Intensity;
    float _FakeLight5_Range;
    float _FakeLight5_AttenuationPower;

    float _FakeLight6_Enabled;
    float4 _FakeLight6_Pos;
    float4 _FakeLight6_Color;
    float _FakeLight6_Intensity;
    float _FakeLight6_Range;
    float _FakeLight6_AttenuationPower;

    float _FakeLight7_Enabled;
    float4 _FakeLight7_Pos;
    float4 _FakeLight7_Color;
    float _FakeLight7_Intensity;
    float _FakeLight7_Range;
    float _FakeLight7_AttenuationPower;

    float _FakeLight8_Enabled;
    float4 _FakeLight8_Pos;
    float4 _FakeLight8_Color;
    float _FakeLight8_Intensity;
    float _FakeLight8_Range;
    float _FakeLight8_AttenuationPower;
CBUFFER_END

// ========== Shared Structures ==========
struct LitAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct LitVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
    float4 tangentWS : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// ========== Shared Vertex Function ==========
LitVaryings LitVertex(LitAttributes input)
{
    LitVaryings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.positionCS = positionInputs.positionCS;
    output.positionWS = positionInputs.positionWS;
    output.normalWS = normalInputs.normalWS;
    output.tangentWS = float4(normalInputs.tangentWS, input.tangentOS.w);
    output.uv = input.uv * _MainTiling.xy + _MainTiling.zw;

    return output;
}

// ========== Shared Fragment Function ==========
half4 LitFragment(LitVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);

    // Sample textures
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    float4 albedo = baseMap * _BaseColor;

    #ifdef _ALPHATEST_ON
        clip(albedo.a - _Cutoff);
    #endif

    // Metallic and Smoothness
    float4 metallicMap = SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, input.uv);
    float metallic = metallicMap.r * _Metallic;
    float smoothness = metallicMap.a * _Smoothness;

    // Occlusion (AO)
    float occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).r;
    occlusion = lerp(1.0, occlusion, _OcclusionStrength);

    // Normal mapping
    float3 normalWS = normalize(input.normalWS);
    #ifdef _USE_NORMAL_MAP
        float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), _NormalScale);
        float3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
        float3x3 TBN = float3x3(input.tangentWS.xyz, bitangentWS, input.normalWS);
        normalWS = normalize(mul(normalTS, TBN));
    #endif

    // Lighting setup
    InputData inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = normalize(_WorldSpaceCameraPos - input.positionWS);
    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);

    // 自定义光照颜色（覆盖场景光照）
    #ifdef _USE_CUSTOM_LIGHTING
        // 使用自定义光照颜色（覆盖场景光照）
        _MainLightColor.rgb = _CustomLightColor.rgb;
        // 使用自定义环境光
        inputData.bakedGI = _CustomAmbientColor.rgb;
    #else
        // 使用场景环境光（球谐函数）
        inputData.bakedGI = SampleSH(normalWS);
    #endif

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = albedo.rgb;
    surfaceData.alpha = albedo.a;
    surfaceData.metallic = metallic;
    surfaceData.smoothness = smoothness;
    surfaceData.normalTS = float3(0, 0, 1);
    surfaceData.occlusion = occlusion;

    // Emission
    #ifdef _USE_EMISSION
        float3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb;
        surfaceData.emission = emission;
    #else
        surfaceData.emission = 0;
    #endif

    // Calculate PBR lighting
    half4 color = UniversalFragmentPBR(inputData, surfaceData);

    // 风格化光照处理（卡通着色）
    #ifdef _USE_STYLIZED_LIGHTING
        // 获取主光源
        Light mainLight = GetMainLight(inputData.shadowCoord);
        half NdotL = saturate(dot(inputData.normalWS, mainLight.direction));

        // 应用风格化漫反射
        half stylizedNdotL = StylizedDiffuse(NdotL, _ShadowSteps, _ShadowSmoothness);

        // 重新计算光照（简化的卡通着色）
        half3 stylizedDiffuse = albedo.rgb * mainLight.color * stylizedNdotL * mainLight.shadowAttenuation;
        half3 stylizedAmbient = albedo.rgb * inputData.bakedGI;

        // 替换原有光照
        color.rgb = stylizedDiffuse + stylizedAmbient;

        // 保留自发光
        #ifdef _USE_EMISSION
            color.rgb += surfaceData.emission;
        #endif
    #endif

    // Rim Light (边缘光)
    #ifdef _USE_RIM_LIGHT
        half NdotV = saturate(dot(inputData.normalWS, inputData.viewDirectionWS));
        half rimMask = 1.0 - NdotV;
        rimMask = pow(rimMask, _RimPower);
        half3 rimLight = rimMask * _RimColor.rgb * _RimIntensity;
        color.rgb += rimLight;
    #endif

    // Fake Point Light (假点光源)
    #ifdef _USE_FAKE_POINT_LIGHT
        half3 totalFakeLight = half3(0, 0, 0);

        // 定义光源计算函数（使用Unity风格的衰减公式）
        #define CALCULATE_FAKE_LIGHT(enabled, pos, color, intensity, range, attenPower) \
        if (enabled > 0.5) { \
            float3 lightPos = pos.xyz; \
            float3 lightVector = lightPos - input.positionWS; \
            float distanceSqr = dot(lightVector, lightVector); \
            float rangeSqr = range * range; \
            \
            /* Unity风格的衰减计算 */ \
            /* 1. 距离衰减（物理准确的平方反比，带最小值避免除零） */ \
            float distanceAttenuation = 1.0 / max(distanceSqr, 0.01 * 0.01); \
            \
            /* 2. 范围衰减（平滑衰减到0） */ \
            float rangeAttenuation = saturate(1.0 - (distanceSqr / rangeSqr)); \
            rangeAttenuation = pow(rangeAttenuation, attenPower); \
            \
            /* 3. 最终衰减 */ \
            float attenuation = distanceAttenuation * rangeAttenuation; \
            \
            if (attenuation > 0.001) { \
                float3 lightDir = normalize(lightVector); \
                float NdotL = max(0.0, dot(inputData.normalWS, lightDir)); \
                totalFakeLight += color.rgb * intensity * NdotL * attenuation; \
            } \
        }

        // 计算所有假点光源
        CALCULATE_FAKE_LIGHT(_FakeLight1_Enabled, _FakeLight1_Pos, _FakeLight1_Color, _FakeLight1_Intensity, _FakeLight1_Range, _FakeLight1_AttenuationPower)
        CALCULATE_FAKE_LIGHT(_FakeLight2_Enabled, _FakeLight2_Pos, _FakeLight2_Color, _FakeLight2_Intensity, _FakeLight2_Range, _FakeLight2_AttenuationPower)
        CALCULATE_FAKE_LIGHT(_FakeLight3_Enabled, _FakeLight3_Pos, _FakeLight3_Color, _FakeLight3_Intensity, _FakeLight3_Range, _FakeLight3_AttenuationPower)
        CALCULATE_FAKE_LIGHT(_FakeLight4_Enabled, _FakeLight4_Pos, _FakeLight4_Color, _FakeLight4_Intensity, _FakeLight4_Range, _FakeLight4_AttenuationPower)
        CALCULATE_FAKE_LIGHT(_FakeLight5_Enabled, _FakeLight5_Pos, _FakeLight5_Color, _FakeLight5_Intensity, _FakeLight5_Range, _FakeLight5_AttenuationPower)
        CALCULATE_FAKE_LIGHT(_FakeLight6_Enabled, _FakeLight6_Pos, _FakeLight6_Color, _FakeLight6_Intensity, _FakeLight6_Range, _FakeLight6_AttenuationPower)
        CALCULATE_FAKE_LIGHT(_FakeLight7_Enabled, _FakeLight7_Pos, _FakeLight7_Color, _FakeLight7_Intensity, _FakeLight7_Range, _FakeLight7_AttenuationPower)
        CALCULATE_FAKE_LIGHT(_FakeLight8_Enabled, _FakeLight8_Pos, _FakeLight8_Color, _FakeLight8_Intensity, _FakeLight8_Range, _FakeLight8_AttenuationPower)

        // 将假点光源添加到最终颜色（乘以albedo实现正确的光照效果）
        color.rgb += albedo.rgb * totalFakeLight;
    #endif

    return color;
}

#endif // CUSTOM_URP_LIT_COMMON_INCLUDED
