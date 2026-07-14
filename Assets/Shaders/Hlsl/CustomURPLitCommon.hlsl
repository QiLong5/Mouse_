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

    // Fake Lights
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

    float _FakeLight9_Enabled;
    float4 _FakeLight9_Pos;
    float4 _FakeLight9_Color;
    float _FakeLight9_Intensity;
    float _FakeLight9_Range;
    float _FakeLight9_AttenuationPower;

    float _FakeLight10_Enabled;
    float4 _FakeLight10_Pos;
    float4 _FakeLight10_Color;
    float _FakeLight10_Intensity;
    float _FakeLight10_Range;
    float _FakeLight10_AttenuationPower;

    float _FakeLight11_Enabled;
    float4 _FakeLight11_Pos;
    float4 _FakeLight11_Color;
    float _FakeLight11_Intensity;
    float _FakeLight11_Range;
    float _FakeLight11_AttenuationPower;

    float _FakeLight12_Enabled;
    float4 _FakeLight12_Pos;
    float4 _FakeLight12_Color;
    float _FakeLight12_Intensity;
    float _FakeLight12_Range;
    float _FakeLight12_AttenuationPower;
CBUFFER_END

// ========== Base Albedo Sampling ==========
// 统一的基础反照率采样，供 Forward / ShadowCaster / Meta 三个 Pass 共用，
// 确保各 Pass 的 Alpha 裁剪与可见表面一致。
// 注意：_USE_CHANNEL_BLEND 及相关通道混合属性目前为占位（主 Pass 从未实现真正的通道混合，
// 且无任何材质启用该开关），故此处与主 Pass 行为保持一致——直接采样 _BaseMap * _BaseColor。
// 若日后要启用真正的通道混合，需同时为 Forward/ShadowCaster/Meta 添加相同的通道贴图声明与
// UnityPerMaterial CBUFFER 字段（保持三 Pass 一致，避免破坏 SRP Batcher）。
float4 SampleChannelBlendedBase(float2 uv)
{
    return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
}

// ========== Shared Structures ==========
struct LitAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
    float4 color : COLOR; // 顶点颜色（用于烘焙光照）
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct LitVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    // WebGL优化：使用half精度，减少varying数量和精度压力
    half3 normalWS : TEXCOORD1;
    #if defined(_USE_NORMAL_MAP)
        half4 tangentWS : TEXCOORD2; // 仅在需要法线贴图时使用
    #endif
    float3 positionWS : TEXCOORD3; // 保持float精度用于阴影和光照计算
    #if defined(_USE_FAKE_POINT_LIGHT) && defined(_USE_NORMAL_MAP)
        half3 geometricNormalWS : TEXCOORD4; // 假光源专用几何法线（不受法线贴图影响）
    #endif
    half4 vertexColor : COLOR; // 烘焙的顶点颜色光照（始终传递，避免WebGL varying不匹配）
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
    output.normalWS = half3(normalInputs.normalWS); // 转换为half精度

    #if defined(_USE_NORMAL_MAP)
        // 仅在需要法线贴图时计算切线
        real sign = input.tangentOS.w * GetOddNegativeScale();
        output.tangentWS = half4(normalInputs.tangentWS, sign);
    #endif

    #if defined(_USE_FAKE_POINT_LIGHT) && defined(_USE_NORMAL_MAP)
        // 保存原始几何法线，供假光源使用（不受法线贴图影响）
        output.geometricNormalWS = half3(normalInputs.normalWS);
    #endif

    // 始终传递顶点颜色（即使不使用也传递，避免WebGL varying不匹配）
    output.vertexColor = input.color;

    output.uv = input.uv * _MainTiling.xy + _MainTiling.zw;

    return output;
}

// ========== Shared Fragment Function ==========
// WebGL兼容性：显式声明fragment shader输出结构
struct FragmentOutput
{
    half4 color : SV_Target0;
};

FragmentOutput LitFragment(LitVaryings input)
{
    UNITY_SETUP_INSTANCE_ID(input);

    FragmentOutput output;

    // 【调试禁用】取消下面这行注释可以禁用整个shader，返回红色
    // output.color = half4(1, 0, 0, 1);
    // return output;

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
    #ifdef _USE_NORMAL_MAP
        half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), _NormalScale);

        // 构建TBN矩阵
        half sgn = input.tangentWS.w;
        half3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
        half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);

        half3 normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
        normalWS = NormalizeNormalPerPixel(normalWS);
    #else
        half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
    #endif

    // Lighting setup
    InputData inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.normalWS = normalWS;
    // 使用URP标准的视角方向计算（WebGL优化版本）
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    // 雾效坐标
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), 0);

    // 自定义光照颜色（覆盖场景光照）
    #ifdef _USE_CUSTOM_LIGHTING
        _MainLightColor.rgb = _CustomLightColor.rgb;
        inputData.bakedGI = _CustomAmbientColor.rgb;
    #else
        inputData.bakedGI = SampleSH(normalWS);
    #endif

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = albedo.rgb;
    surfaceData.alpha = albedo.a;
    surfaceData.metallic = metallic;
    surfaceData.smoothness = smoothness;
    surfaceData.normalTS = float3(0, 0, 1);
    surfaceData.occlusion = occlusion;

    // Emission (WebGL HDR精度优化：使用float避免half精度损失)
    #ifdef _USE_EMISSION
        float3 emissionFloat = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb;
        surfaceData.emission = emissionFloat;
    #else
        surfaceData.emission = 0;
    #endif

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

    // Rim Light (边缘光) - WebGL2精度修复版本
    #ifdef _USE_RIM_LIGHT
        // 使用float精度避免WebGL2中half精度导致pow()计算失真
        float NdotV_rim = saturate(dot((float3)normalWS, (float3)inputData.viewDirectionWS));
        float rimMask = saturate(1.0 - NdotV_rim);
        // 用exp2+log替代pow，在WebGL2上精度更稳定
        rimMask = exp2(log2(max(rimMask, 1e-6)) * _RimPower);
        half3 rimLight = half3(rimMask * _RimColor.rgb * _RimIntensity);
        color.rgb += rimLight;
    #endif

    // 假点光源计算（或使用烘焙的顶点颜色）
    #if defined(_USE_VERTEX_COLOR)
        // 使用烘焙的顶点颜色光照（预计算，0开销）
        half3 bakedLighting = input.vertexColor.rgb;
        color.rgb += albedo.rgb * bakedLighting;
    #elif defined(_USE_FAKE_POINT_LIGHT)
        // 实时计算假点光源（高开销）
        // 内联的假光源计算函数（避免include导致的编译错误）
        half3 totalFakeLight = half3(0, 0, 0);

        // 决定使用哪个法线：如果启用了法线贴图，使用几何法线；否则使用当前法线
        #if defined(_USE_NORMAL_MAP)
            // 使用几何法线（不受法线贴图影响），确保假光源基于真实几何形状计算
            half3 fakeLightNormal = normalize(input.geometricNormalWS);
        #else
            // 没有法线贴图时，直接使用当前法线
            half3 fakeLightNormal = normalWS;
        #endif

        // 光源计算宏
        #define CALCULATE_FAKE_LIGHT(enabled, pos, color, intensity, range, attenPower) \
        if (enabled > 0.5) { \
            float3 lightPos = pos; \
            float3 lightVector = lightPos - inputData.positionWS; \
            float distanceSqr = dot(lightVector, lightVector); \
            float rangeSqr = range * range; \
            \
            if (distanceSqr < rangeSqr) { \
                float distanceAttenuation = 1.0 / max(distanceSqr, 0.01 * 0.01); \
                float rangeAttenuation = saturate(1.0 - (distanceSqr / rangeSqr)); \
                rangeAttenuation = pow(rangeAttenuation, attenPower); \
                float attenuation = distanceAttenuation * rangeAttenuation; \
                \
                if (attenuation > 0.001) { \
                    float3 lightDir = normalize(lightVector); \
                    float NdotL = max(0.0, dot(fakeLightNormal, lightDir)); \
                    totalFakeLight += color * intensity * NdotL * attenuation; \
                } \
            } \
        }

        // 计算所有12个假点光源
        CALCULATE_FAKE_LIGHT(_FakeLight1_Enabled, _FakeLight1_Pos.xyz, _FakeLight1_Color.rgb, _FakeLight1_Intensity, _FakeLight1_Range, _FakeLight1_AttenuationPower)
        CALCULATE_FAKE_LIGHT(_FakeLight2_Enabled, _FakeLight2_Pos.xyz, _FakeLight2_Color.rgb, _FakeLight2_Intensity, _FakeLight2_Range, _FakeLight2_AttenuationPower)
        CALCULATE_FAKE_LIGHT(_FakeLight3_Enabled, _FakeLight3_Pos.xyz, _FakeLight3_Color.rgb, _FakeLight3_Intensity, _FakeLight3_Range, _FakeLight3_AttenuationPower)
        CALCULATE_FAKE_LIGHT(_FakeLight4_Enabled, _FakeLight4_Pos.xyz, _FakeLight4_Color.rgb, _FakeLight4_Intensity, _FakeLight4_Range, _FakeLight4_AttenuationPower)
        CALCULATE_FAKE_LIGHT(_FakeLight5_Enabled, _FakeLight5_Pos.xyz, _FakeLight5_Color.rgb, _FakeLight5_Intensity, _FakeLight5_Range, _FakeLight5_AttenuationPower)
        CALCULATE_FAKE_LIGHT(_FakeLight6_Enabled, _FakeLight6_Pos.xyz, _FakeLight6_Color.rgb, _FakeLight6_Intensity, _FakeLight6_Range, _FakeLight6_AttenuationPower)
        CALCULATE_FAKE_LIGHT(_FakeLight7_Enabled, _FakeLight7_Pos.xyz, _FakeLight7_Color.rgb, _FakeLight7_Intensity, _FakeLight7_Range, _FakeLight7_AttenuationPower)
        CALCULATE_FAKE_LIGHT(_FakeLight8_Enabled, _FakeLight8_Pos.xyz, _FakeLight8_Color.rgb, _FakeLight8_Intensity, _FakeLight8_Range, _FakeLight8_AttenuationPower)
        CALCULATE_FAKE_LIGHT(_FakeLight9_Enabled, _FakeLight9_Pos.xyz, _FakeLight9_Color.rgb, _FakeLight9_Intensity, _FakeLight9_Range, _FakeLight9_AttenuationPower)
        CALCULATE_FAKE_LIGHT(_FakeLight10_Enabled, _FakeLight10_Pos.xyz, _FakeLight10_Color.rgb, _FakeLight10_Intensity, _FakeLight10_Range, _FakeLight10_AttenuationPower)
        CALCULATE_FAKE_LIGHT(_FakeLight11_Enabled, _FakeLight11_Pos.xyz, _FakeLight11_Color.rgb, _FakeLight11_Intensity, _FakeLight11_Range, _FakeLight11_AttenuationPower)
        CALCULATE_FAKE_LIGHT(_FakeLight12_Enabled, _FakeLight12_Pos.xyz, _FakeLight12_Color.rgb, _FakeLight12_Intensity, _FakeLight12_Range, _FakeLight12_AttenuationPower)

        #undef CALCULATE_FAKE_LIGHT

        // 叠加假光源贡献
        color.rgb += albedo.rgb * totalFakeLight;
    #endif

    // 应用雾效（Fog）
    color.rgb = MixFog(color.rgb, inputData.fogCoord);

    // 预乘alpha输出：兼容WebGL canvas premultipliedAlpha:true的合成行为
    // Blend模式为 One/OneMinusSrcAlpha，RGB需要提前乘以alpha
    output.color = half4(color.rgb * albedo.a, albedo.a);
    return output;
}

#endif // CUSTOM_URP_LIT_COMMON_INCLUDED
