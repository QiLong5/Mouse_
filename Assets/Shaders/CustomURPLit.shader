Shader "Custom/URP Lit Advanced"
{
    Properties
    {
        [Header(Render Settings)]
        [Enum(Opaque,0,Transparent,1)] _RenderMode("渲染模式", Float) = 0
        [Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows("接受阴影", Float) = 1
        [Toggle(_ALPHATEST_ON)] _AlphaClip("透明度裁剪", Float) = 0
        _Cutoff("裁剪阈值", Range(0,1)) = 0.5
        [Toggle] _DoubleSided("双面渲染", Float) = 0

        [Header(Base)]
        _BaseMap("基础贴图", 2D) = "white" {}
        _BaseColor("基础颜色", Color) = (1,1,1,1)

        [Header(Outline)]
        [Toggle(_ENABLE_OUTLINE)] _EnableOutline("启用描边", Float) = 0
        [Toggle(_USE_VERTEX_COLOR)] _UseVertexColor("使用顶点色平滑法线", Float) = 0
        _OutlineWidth("描边粗细", Range(0, 0.1)) = 0.01
        _OutlineColor("描边颜色", Color) = (0,0,0,1)

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

        [Header(Stylized Lighting)]
        [Toggle(_USE_STYLIZED_LIGHTING)] _UseStylizedLighting("启用风格化光照", Float) = 0
        _ShadowSteps("阴影分层数", Range(1,5)) = 2
        _ShadowSmoothness("阴影平滑度", Range(0,1)) = 0.1

        [Header(Custom Lighting)]
        [Toggle(_USE_CUSTOM_LIGHTING)] _UseCustomLighting("使用自定义光照颜色", Float) = 0
        [HDR] _CustomLightColor("自定义光源颜色", Color) = (1,1,1,1)
        [HDR] _CustomAmbientColor("自定义环境光颜色", Color) = (0.2,0.2,0.2,1)

        [Header(Fake Point Lights)]
        [Toggle(_USE_FAKE_POINT_LIGHT)] _UseFakePointLight("启用假点光源", Float) = 0
        [HideInInspector] _FakeLightCount("光源数量", Int) = 1

        // Light 1
        [Toggle] _FakeLight1_Enabled ("光源1启用", Float) = 1
        _FakeLight1_Pos ("光源1位置", Vector) = (0, 5, 0, 0)
        _FakeLight1_Color ("光源1颜色", Color) = (1, 0.9, 0.7, 1)
        _FakeLight1_Intensity ("光源1强度", Range(0, 20)) = 1
        _FakeLight1_Range ("光源1范围", Range(1, 50)) = 10
        _FakeLight1_AttenuationPower ("光源1衰减", Range(1, 4)) = 2

        // Light 2
        [Toggle] _FakeLight2_Enabled ("光源2启用", Float) = 0
        _FakeLight2_Pos ("光源2位置", Vector) = (5, 5, 0, 0)
        _FakeLight2_Color ("光源2颜色", Color) = (0.7, 0.9, 1, 1)
        _FakeLight2_Intensity ("光源2强度", Range(0, 20)) = 1
        _FakeLight2_Range ("光源2范围", Range(1, 50)) = 10
        _FakeLight2_AttenuationPower ("光源2衰减", Range(1, 4)) = 2

        // Light 3
        [Toggle] _FakeLight3_Enabled ("光源3启用", Float) = 0
        _FakeLight3_Pos ("光源3位置", Vector) = (-5, 5, 0, 0)
        _FakeLight3_Color ("光源3颜色", Color) = (1, 0.7, 0.9, 1)
        _FakeLight3_Intensity ("光源3强度", Range(0, 20)) = 1
        _FakeLight3_Range ("光源3范围", Range(1, 50)) = 10
        _FakeLight3_AttenuationPower ("光源3衰减", Range(1, 4)) = 2

        // Light 4
        [Toggle] _FakeLight4_Enabled ("光源4启用", Float) = 0
        _FakeLight4_Pos ("光源4位置", Vector) = (0, 5, 5, 0)
        _FakeLight4_Color ("光源4颜色", Color) = (0.9, 1, 0.7, 1)
        _FakeLight4_Intensity ("光源4强度", Range(0, 20)) = 1
        _FakeLight4_Range ("光源4范围", Range(1, 50)) = 10
        _FakeLight4_AttenuationPower ("光源4衰减", Range(1, 4)) = 2

        // Light 5
        [Toggle] _FakeLight5_Enabled ("光源5启用", Float) = 0
        _FakeLight5_Pos ("光源5位置", Vector) = (0, 5, -5, 0)
        _FakeLight5_Color ("光源5颜色", Color) = (1, 1, 0.7, 1)
        _FakeLight5_Intensity ("光源5强度", Range(0, 20)) = 1
        _FakeLight5_Range ("光源5范围", Range(1, 50)) = 10
        _FakeLight5_AttenuationPower ("光源5衰减", Range(1, 4)) = 2

        // Light 6
        [Toggle] _FakeLight6_Enabled ("光源6启用", Float) = 0
        _FakeLight6_Pos ("光源6位置", Vector) = (-5, 5, 5, 0)
        _FakeLight6_Color ("光源6颜色", Color) = (0.7, 1, 1, 1)
        _FakeLight6_Intensity ("光源6强度", Range(0, 20)) = 1
        _FakeLight6_Range ("光源6范围", Range(1, 50)) = 10
        _FakeLight6_AttenuationPower ("光源6衰减", Range(1, 4)) = 2

        // Light 7
        [Toggle] _FakeLight7_Enabled ("光源7启用", Float) = 0
        _FakeLight7_Pos ("光源7位置", Vector) = (5, 5, 5, 0)
        _FakeLight7_Color ("光源7颜色", Color) = (1, 0.8, 0.9, 1)
        _FakeLight7_Intensity ("光源7强度", Range(0, 20)) = 1
        _FakeLight7_Range ("光源7范围", Range(1, 50)) = 10
        _FakeLight7_AttenuationPower ("光源7衰减", Range(1, 4)) = 2

        // Light 8
        [Toggle] _FakeLight8_Enabled ("光源8启用", Float) = 0
        _FakeLight8_Pos ("光源8位置", Vector) = (5, 5, -5, 0)
        _FakeLight8_Color ("光源8颜色", Color) = (0.9, 0.8, 1, 1)
        _FakeLight8_Intensity ("光源8强度", Range(0, 20)) = 1
        _FakeLight8_Range ("光源8范围", Range(1, 50)) = 10
        _FakeLight8_AttenuationPower ("光源8衰减", Range(1, 4)) = 2

        [Header(Tiling And Offset)]
        _MainTiling("主纹理平铺", Vector) = (1,1,0,0)

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

        // Pass 1: Outline - Render FIRST (expanded back faces)
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual
            Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ENABLE_OUTLINE
            #pragma shader_feature_local _USE_VERTEX_COLOR
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _MainTiling;
                float _OutlineWidth;
                float4 _OutlineColor;
                float _Cutoff;
            CBUFFER_END

            Varyings OutlineVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.uv = input.uv * _MainTiling.xy + _MainTiling.zw;

                #ifdef _ENABLE_OUTLINE
                    float3 normalOS = input.normalOS;

                    #ifdef _USE_VERTEX_COLOR
                        // Use vertex color as smoothed normal
                        normalOS = input.color.rgb * 2.0 - 1.0;
                    #endif

                    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                    float3 normalWS = TransformObjectToWorldNormal(normalOS);
                    positionWS += normalize(normalWS) * _OutlineWidth;
                    output.positionCS = TransformWorldToHClip(positionWS);
                #else
                    // If outline disabled, position off-screen to skip rendering
                    output.positionCS = float4(0, 0, 0, 0);
                #endif

                return output;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                #ifndef _ENABLE_OUTLINE
                    discard;
                #endif

                float4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float alpha = baseColor.a * _BaseColor.a;

                #ifdef _ALPHATEST_ON
                    clip(alpha - _Cutoff);
                #else
                    clip(alpha - 0.01);
                #endif

                return half4(_OutlineColor.rgb, _OutlineColor.a * alpha);
            }
            ENDHLSL
        }

        // Pass 2: ForwardLit - Main rendering pass
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
            #pragma multi_compile_fog

            // Main light shadows
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            // Additional lights (simplified for mobile)
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            // Shadows (mobile optimized)
            #pragma multi_compile _ _SHADOWS_SOFT

            // Lightmap support (optional, can disable for better performance)
            #pragma multi_compile _ LIGHTMAP_ON

            // Local shader features
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _RECEIVE_SHADOWS
            #pragma shader_feature_local _USE_NORMAL_MAP
            #pragma shader_feature_local _USE_EMISSION
            #pragma shader_feature_local _USE_RIM_LIGHT
            #pragma shader_feature_local _USE_STYLIZED_LIGHTING
            #pragma shader_feature_local _USE_CUSTOM_LIGHTING
            #pragma shader_feature_local _USE_FAKE_POINT_LIGHT

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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _MainTiling;
                float _Cutoff;
            CBUFFER_END

            float3 _LightDirection;
            float3 _LightPosition;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

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
                    float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                    clip(alpha - _Cutoff);
                #endif

                return 0;
            }
            ENDHLSL
        }

        // Depth Only Pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _MainTiling;
                float _Cutoff;
            CBUFFER_END

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * _MainTiling.xy + _MainTiling.zw;

                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                #ifdef _ALPHATEST_ON
                    float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                    clip(alpha - _Cutoff);
                #endif

                return 0;
            }
            ENDHLSL
        }

        // DepthNormals Pass - Required for SSAO and other post-processing effects
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _USE_NORMAL_MAP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                #ifdef _USE_NORMAL_MAP
                    float4 tangentWS : TEXCOORD2;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _MainTiling;
                float _Cutoff;
                float _NormalScale;
            CBUFFER_END

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positionInputs.positionCS;
                output.normalWS = normalInputs.normalWS;
                #ifdef _USE_NORMAL_MAP
                    output.tangentWS = float4(normalInputs.tangentWS, input.tangentOS.w);
                #endif
                output.uv = input.uv * _MainTiling.xy + _MainTiling.zw;

                return output;
            }

            half4 DepthNormalsFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                #ifdef _ALPHATEST_ON
                    float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                    clip(alpha - _Cutoff);
                #endif

                float3 normalWS = normalize(input.normalWS);

                #ifdef _USE_NORMAL_MAP
                    float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), _NormalScale);
                    float3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                    float3x3 TBN = float3x3(input.tangentWS.xyz, bitangentWS, input.normalWS);
                    normalWS = normalize(mul(normalTS, TBN));
                #endif

                return half4(normalWS, 0);
            }
            ENDHLSL
        }

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
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _EmissionColor;
                float4 _MainTiling;
                float _Cutoff;
                float _Metallic;
                float _Smoothness;
            CBUFFER_END

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
                float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

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
