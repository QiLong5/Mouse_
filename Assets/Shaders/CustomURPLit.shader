Shader "Custom/URP Lit Advanced"
{
    Properties
    {
        [Header(Render Settings)]
        [Enum(Opaque,0,Transparent,1)] _RenderMode("渲染模式", Float) = 0
        [Toggle] _ReceiveShadows("接受阴影", Float) = 1
        [Toggle(_ALPHATEST_ON)] _AlphaClip("透明度裁剪", Float) = 0
        _Cutoff("裁剪阈值", Range(0,1)) = 0.5
        [Toggle] _DoubleSided("双面渲染", Float) = 0

        [Header(Base)]
        _BaseMap("基础贴图", 2D) = "white" {}
        _BaseColor("基础颜色", Color) = (1,1,1,1)

        [Header(RGB Channel Blend)]
        [Toggle(_USE_CHANNEL_BLEND)] _UseChannelBlend("Use RGB Channel Blend", Float) = 0
        [NoScaleOffset] _ChannelMap("Channel Map (RGB)", 2D) = "white" {}
        _BlendMapG("Green Channel Texture", 2D) = "white" {}
        _BlendMapB("Blue Channel Texture", 2D) = "white" {}
        _BlendColorG("Green Channel Tint", Color) = (1,1,1,1)
        _BlendColorB("Blue Channel Tint", Color) = (1,1,1,1)
        _ChannelBlendSharpness("Blend Sharpness", Range(0.25, 8)) = 1

        [Header(Surface)]
        _MetallicMap("金属度贴图", 2D) = "white" {}
        _Metallic("金属度", Range(0,1)) = 0
        _Smoothness("光滑度", Range(0,1)) = 0.5
        _OcclusionMap("环境光遮蔽贴图 (AO)", 2D) = "white" {}
        _OcclusionStrength("AO强度", Range(0,1)) = 1.0

        [Header(Normal)]
        [Toggle(_USE_NORMAL_MAP)] _UseNormalMap("使用法线贴图", Float) = 0
        _NormalMap("法线贴图", 2D) = "bump" {}
        _NormalScale("法线强度", Range(0,2)) = 1

        [Header(Emission)]
        [Toggle(_USE_EMISSION)] _UseEmission("使用自发光", Float) = 0
        _EmissionMap("自发光贴图", 2D) = "white" {}
        [HDR] _EmissionColor("自发光颜色", Color) = (0,0,0,1)

        [Header(Rim Light)]
        [Toggle(_USE_RIM_LIGHT)] _UseRimLight("启用边缘光", Float) = 0
        [HDR] _RimColor("边缘光颜色", Color) = (1,1,1,1)
        _RimPower("边缘光范围", Range(0,10)) = 3.0
        _RimIntensity("边缘光强度", Range(0,5)) = 1.0

        [Header(Custom Lighting)]
        [Toggle(_USE_CUSTOM_LIGHTING)] _UseCustomLighting("使用自定义光照颜色", Float) = 0
        [HDR] _CustomLightColor("自定义光源颜色", Color) = (1,1,1,1)
        [HDR] _CustomAmbientColor("自定义环境光颜色", Color) = (0.2,0.2,0.2,1)

        [Header(Tiling And Offset)]
        _MainTiling("主纹理平铺", Vector) = (1,1,0,0)

        [Header(Fake Point Lights)]
        [Toggle(_USE_FAKE_POINT_LIGHT)] _UseFakePointLight("启用假点光源", Float) = 0
        [Toggle(_USE_VERTEX_COLOR)] _UseVertexColor("使用顶点颜色光照（烘焙）", Float) = 0

        _FakeLight1_Enabled ("光源1启用", Float) = 0
        _FakeLight1_Pos ("光源1位置", Vector) = (0, 5, 0, 0)
        _FakeLight1_Color ("光源1颜色", Color) = (1, 0.9, 0.7, 1)
        _FakeLight1_Intensity ("光源1强度", Float) = 1
        _FakeLight1_Range ("光源1范围", Range(1, 50)) = 10
        _FakeLight1_AttenuationPower ("光源1衰减", Range(1, 4)) = 2

        _FakeLight2_Enabled ("光源2启用", Float) = 0
        _FakeLight2_Pos ("光源2位置", Vector) = (5, 5, 0, 0)
        _FakeLight2_Color ("光源2颜色", Color) = (0.7, 0.9, 1, 1)
        _FakeLight2_Intensity ("光源2强度", Float) = 1
        _FakeLight2_Range ("光源2范围", Range(1, 50)) = 10
        _FakeLight2_AttenuationPower ("光源2衰减", Range(1, 4)) = 2

        _FakeLight3_Enabled ("光源3启用", Float) = 0
        _FakeLight3_Pos ("光源3位置", Vector) = (-5, 5, 0, 0)
        _FakeLight3_Color ("光源3颜色", Color) = (1, 0.7, 0.9, 1)
        _FakeLight3_Intensity ("光源3强度", Float) = 1
        _FakeLight3_Range ("光源3范围", Range(1, 50)) = 10
        _FakeLight3_AttenuationPower ("光源3衰减", Range(1, 4)) = 2

        _FakeLight4_Enabled ("光源4启用", Float) = 0
        _FakeLight4_Pos ("光源4位置", Vector) = (0, 5, 5, 0)
        _FakeLight4_Color ("光源4颜色", Color) = (0.9, 1, 0.7, 1)
        _FakeLight4_Intensity ("光源4强度", Float) = 1
        _FakeLight4_Range ("光源4范围", Range(1, 50)) = 10
        _FakeLight4_AttenuationPower ("光源4衰减", Range(1, 4)) = 2

        _FakeLight5_Enabled ("光源5启用", Float) = 0
        _FakeLight5_Pos ("光源5位置", Vector) = (0, 5, -5, 0)
        _FakeLight5_Color ("光源5颜色", Color) = (1, 1, 0.7, 1)
        _FakeLight5_Intensity ("光源5强度", Float) = 1
        _FakeLight5_Range ("光源5范围", Range(1, 50)) = 10
        _FakeLight5_AttenuationPower ("光源5衰减", Range(1, 4)) = 2

        _FakeLight6_Enabled ("光源6启用", Float) = 0
        _FakeLight6_Pos ("光源6位置", Vector) = (-5, 5, 5, 0)
        _FakeLight6_Color ("光源6颜色", Color) = (0.7, 1, 1, 1)
        _FakeLight6_Intensity ("光源6强度", Float) = 1
        _FakeLight6_Range ("光源6范围", Range(1, 50)) = 10
        _FakeLight6_AttenuationPower ("光源6衰减", Range(1, 4)) = 2

        _FakeLight7_Enabled ("光源7启用", Float) = 0
        _FakeLight7_Pos ("光源7位置", Vector) = (5, 5, 5, 0)
        _FakeLight7_Color ("光源7颜色", Color) = (1, 0.8, 0.9, 1)
        _FakeLight7_Intensity ("光源7强度", Float) = 1
        _FakeLight7_Range ("光源7范围", Range(1, 50)) = 10
        _FakeLight7_AttenuationPower ("光源7衰减", Range(1, 4)) = 2

        _FakeLight8_Enabled ("光源8启用", Float) = 0
        _FakeLight8_Pos ("光源8位置", Vector) = (5, 5, -5, 0)
        _FakeLight8_Color ("光源8颜色", Color) = (0.9, 0.8, 1, 1)
        _FakeLight8_Intensity ("光源8强度", Float) = 1
        _FakeLight8_Range ("光源8范围", Range(1, 50)) = 10
        _FakeLight8_AttenuationPower ("光源8衰减", Range(1, 4)) = 2

        _FakeLight9_Enabled ("光源9启用", Float) = 0
        _FakeLight9_Pos ("光源9位置", Vector) = (-5, 5, -5, 0)
        _FakeLight9_Color ("光源9颜色", Color) = (1, 1, 1, 1)
        _FakeLight9_Intensity ("光源9强度", Float) = 1
        _FakeLight9_Range ("光源9范围", Range(1, 50)) = 10
        _FakeLight9_AttenuationPower ("光源9衰减", Range(1, 4)) = 2

        _FakeLight10_Enabled ("光源10启用", Float) = 0
        _FakeLight10_Pos ("光源10位置", Vector) = (0, 10, 0, 0)
        _FakeLight10_Color ("光源10颜色", Color) = (1, 1, 1, 1)
        _FakeLight10_Intensity ("光源10强度", Float) = 1
        _FakeLight10_Range ("光源10范围", Range(1, 50)) = 10
        _FakeLight10_AttenuationPower ("光源10衰减", Range(1, 4)) = 2

        _FakeLight11_Enabled ("光源11启用", Float) = 0
        _FakeLight11_Pos ("光源11位置", Vector) = (10, 5, 0, 0)
        _FakeLight11_Color ("光源11颜色", Color) = (1, 1, 1, 1)
        _FakeLight11_Intensity ("光源11强度", Float) = 1
        _FakeLight11_Range ("光源11范围", Range(1, 50)) = 10
        _FakeLight11_AttenuationPower ("光源11衰减", Range(1, 4)) = 2

        _FakeLight12_Enabled ("光源12启用", Float) = 0
        _FakeLight12_Pos ("光源12位置", Vector) = (-10, 5, 0, 0)
        _FakeLight12_Color ("光源12颜色", Color) = (1, 1, 1, 1)
        _FakeLight12_Intensity ("光源12强度", Float) = 1
        _FakeLight12_Range ("光源12范围", Range(1, 50)) = 10
        _FakeLight12_AttenuationPower ("光源12衰减", Range(1, 4)) = 2

        [Header(Advanced)]
        _QueueOffset("渲染队列偏移", Range(-50, 50)) = 0

        // Hidden properties for render state
        [HideInInspector] _SrcBlend("Src Blend", Float) = 1.0
        [HideInInspector] _DstBlend("Dst Blend", Float) = 0.0
        [HideInInspector] _ZWrite("ZWrite", Float) = 1.0
        [HideInInspector] _Cull("Cull", Float) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        LOD 300

        // Pass 1: ForwardLit - Main rendering pass
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex LitVertex
            #pragma fragment LitFragment

            // Instancing & GPU
            #pragma multi_compile_instancing

            // Main light shadows（已移除 fog[场景关] / CASCADE[级联=1]）
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS

            // 移除额外光源支持
            // #pragma multi_compile _ _ADDITIONAL_LIGHTS

            // 移除额外光源阴影
            // #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

            // 移除软阴影
            // #pragma multi_compile _ _SHADOWS_SOFT

            // 移除光照贴图烘焙
            // #pragma multi_compile _ LIGHTMAP_ON

            // Local shader features
            // _ALPHATEST_ON 改 shader_feature：无材质启用裁剪，只收 off 变体（原 multi_compile 强制 ×2）
            #pragma shader_feature_local _ALPHATEST_ON
            // _RECEIVE_SHADOWS 已删：死关键字，shader/include 无任何 #ifdef 引用
            #pragma shader_feature_local _USE_NORMAL_MAP
            #pragma shader_feature_local _USE_EMISSION
            #pragma shader_feature_local _USE_RIM_LIGHT
            // 移除风格化光照
            // #pragma shader_feature_local _USE_STYLIZED_LIGHTING
            #pragma shader_feature_local _USE_CUSTOM_LIGHTING
            #pragma shader_feature_local _USE_FAKE_POINT_LIGHT
            #pragma shader_feature_local _USE_VERTEX_COLOR
            #pragma shader_feature_local _USE_CHANNEL_BLEND

            #include "Hlsl/CustomURPLitCommon.hlsl"
            ENDHLSL
        }

        // Shadow Caster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _USE_CHANNEL_BLEND

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // 使用与主Pass相同的完整CBUFFER和纹理声明 - SRP Batcher要求
            #include "Hlsl/CustomURPLitCommon.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // 使用URP提供的标准函数，支持GPU Instancing和静态合批
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                float3 positionWS = positionInputs.positionWS;
                float3 normalWS = normalInputs.normalWS;

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                output.uv = input.uv * _MainTiling.xy + _MainTiling.zw;

                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                #ifdef _ALPHATEST_ON
                    float4 albedo = SampleChannelBlendedBase(input.uv);
                    clip(albedo.a - _Cutoff);
                #endif

                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }

        // Depth passes removed for Luna/WebGL performance optimization
        // DepthOnly and DepthNormals passes are not needed on mobile/web platforms
        // If you need these for desktop builds, consider using shader variants

        // Meta Pass - For lightmap baking
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaLit

            #pragma shader_feature_local _USE_EMISSION
            #pragma shader_feature_local _USE_CHANNEL_BLEND
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            // 使用与主Pass相同的完整CBUFFER和纹理声明 - SRP Batcher要求
            #include "Hlsl/CustomURPLitCommon.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings UniversalVertexMeta(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.uv1, input.uv2);
                output.uv = input.uv0 * _MainTiling.xy + _MainTiling.zw;
                return output;
            }

            half4 UniversalFragmentMetaLit(Varyings input) : SV_Target
            {
                float4 albedo = SampleChannelBlendedBase(input.uv);

                #ifdef _ALPHATEST_ON
                    clip(albedo.a - _Cutoff);
                #endif

                MetaInput metaInput = (MetaInput)0;
                metaInput.Albedo = albedo.rgb;

                #ifdef _USE_EMISSION
                    metaInput.Emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb;
                #else
                    metaInput.Emission = 0;
                #endif

                return UnityMetaFragment(metaInput);
            }
            ENDHLSL
        }
    }

    CustomEditor "CustomURPLitShaderGUI"
    // Playworks Plugin only supports Fallback for ShadowCaster
    Fallback Off
}
