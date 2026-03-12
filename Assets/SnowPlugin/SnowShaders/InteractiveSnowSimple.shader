Shader "Custom/InteractiveSnowSimple"
{
    Properties
    {
        [Header(Base Texture)]
        _BaseColor ("底色", Color) = (1,1,1,1)
        _SnowColor ("雪的颜色", Color) = (0.95, 0.95, 0.98, 1)

        [Header(Surface)]
        _Smoothness ("光滑度", Range(0, 1)) = 0.3
        _Metallic ("金属度", Range(0, 1)) = 0.0

        [Header(Height Displacement)]
        _HeightScale ("高度缩放(顶点)", Range(0, 0.5)) = 0.1

        [Header(Snow Emission)]
        _SnowEmission ("雪面发光强度", Range(0, 2)) = 0.0

        [Header(Edge Highlight)]
        _TopEdgeColor ("顶部边缘光颜色", Color) = (0.8, 0.9, 1.0, 1)
        _TopEdgeIntensity ("顶部边缘光强度", Range(0, 3)) = 0.8
        _BottomEdgeColor ("底部边缘光颜色", Color) = (0.5, 0.7, 1.0, 1)
        _BottomEdgeIntensity ("底部边缘光强度", Range(0, 3)) = 1.2
        _EdgeThreshold ("边缘检测灵敏度", Range(0.001, 0.2)) = 0.03

        [HideInInspector] _SnowHeightMap ("Snow Height Map", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
            "DisableBatching" = "True"
        }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 snowUV : TEXCOORD2;
            };

            TEXTURE2D(_SnowHeightMap);
            SAMPLER(sampler_SnowHeightMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _SnowColor;
                float _Smoothness;
                float _Metallic;
                float _HeightScale;
                float _SnowEmission;
                float4 _TopEdgeColor;
                float _TopEdgeIntensity;
                float4 _BottomEdgeColor;
                float _BottomEdgeIntensity;
                float _EdgeThreshold;
            CBUFFER_END

            // Global snow parameters
            float3 _SnowWorldCenter;
            float2 _SnowWorldSize;

            float2 WorldToSnowUV(float3 worldPos)
            {
                float2 localPos = float2(
                    worldPos.x - _SnowWorldCenter.x,
                    worldPos.z - _SnowWorldCenter.z
                );
                return float2(
                    (localPos.x / _SnowWorldSize.x) + 0.5,
                    (localPos.y / _SnowWorldSize.y) + 0.5
                );
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                // 计算世界坐标
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                // 获取雪地UV和高度
                float2 snowUV = WorldToSnowUV(positionWS);
                float height = SAMPLE_TEXTURE2D_LOD(_SnowHeightMap, sampler_SnowHeightMap, snowUV, 0).r;

                // 顶点下沉（基于高度图）
                float displacement = (1.0 - height) * _HeightScale;
                float3 displacedPos = input.positionOS.xyz - input.normalOS * displacement;

                // 变换
                output.positionCS = TransformObjectToHClip(displacedPos);
                output.positionWS = TransformObjectToWorld(displacedPos);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.snowUV = snowUV;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 采样高度图
                float height = SAMPLE_TEXTURE2D(_SnowHeightMap, sampler_SnowHeightMap, input.snowUV).r;

                // 边缘检测：采样周围像素检测梯度
                float texelSize = 1.0 / 1024.0; // HeightMap分辨率，与SnowInteractionManager一致（1024）
                float heightLeft = SAMPLE_TEXTURE2D(_SnowHeightMap, sampler_SnowHeightMap, input.snowUV + float2(-texelSize, 0)).r;
                float heightRight = SAMPLE_TEXTURE2D(_SnowHeightMap, sampler_SnowHeightMap, input.snowUV + float2(texelSize, 0)).r;
                float heightUp = SAMPLE_TEXTURE2D(_SnowHeightMap, sampler_SnowHeightMap, input.snowUV + float2(0, texelSize)).r;
                float heightDown = SAMPLE_TEXTURE2D(_SnowHeightMap, sampler_SnowHeightMap, input.snowUV + float2(0, -texelSize)).r;

                // 计算梯度强度
                float gradientX = heightRight - heightLeft;
                float gradientY = heightUp - heightDown;
                float gradient = length(float2(gradientX, gradientY));

                // 区分顶部边缘和底部边缘
                // 顶部边缘：从高处(1.0)过渡到凹陷的边缘，height在0.6-1.0之间
                // 底部边缘：凹陷底部的边缘，height在0.0-0.4之间
                float topEdgeMask = smoothstep(0.5, 0.9, height); // height越高，顶部边缘越强
                float bottomEdgeMask = smoothstep(0.4, 0.1, height); // height越低，底部边缘越强

                // 计算边缘强度
                float edgeStrength = smoothstep(0, _EdgeThreshold, gradient);

                // 分别计算顶部和底部边缘光
                half3 topEdge = _TopEdgeColor.rgb * edgeStrength * topEdgeMask * _TopEdgeIntensity;
                half3 bottomEdge = _BottomEdgeColor.rgb * edgeStrength * bottomEdgeMask * _BottomEdgeIntensity;

                // 混合颜色（被踩的地方显示底色）
                float snowBlend = smoothstep(0.3, 0.8, height); // height越高(未踩踏)，越接近雪色
                half3 albedo = lerp(_BaseColor.rgb, _SnowColor.rgb, snowBlend);

                // 使用标准PBR光照
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.normalWS = normalize(input.normalWS);
                lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.alpha = 1.0;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.occlusion = 1.0;

                // 计算总自发光：雪面发光 + 顶部边缘光 + 底部边缘光
                half3 snowEmission = _SnowColor.rgb * _SnowEmission * snowBlend; // 只有雪面发光
                surfaceData.emission = snowEmission + topEdge + bottomEdge;

                return UniversalFragmentPBR(lightingInput, surfaceData);
            }
            ENDHLSL
        }

        // 简化的阴影Pass
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
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            TEXTURE2D(_SnowHeightMap);
            SAMPLER(sampler_SnowHeightMap);

            float _HeightScale;
            float3 _SnowWorldCenter;
            float2 _SnowWorldSize;

            float2 WorldToSnowUV(float3 worldPos)
            {
                float2 localPos = float2(worldPos.x - _SnowWorldCenter.x, worldPos.z - _SnowWorldCenter.z);
                return float2((localPos.x / _SnowWorldSize.x) + 0.5, (localPos.y / _SnowWorldSize.y) + 0.5);
            }

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float2 snowUV = WorldToSnowUV(positionWS);
                float height = SAMPLE_TEXTURE2D_LOD(_SnowHeightMap, sampler_SnowHeightMap, snowUV, 0).r;

                float displacement = (1.0 - height) * _HeightScale;
                float3 displacedPos = input.positionOS.xyz - input.normalOS * displacement;

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformObjectToHClip(displacedPos);

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // DepthOnly Pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            TEXTURE2D(_SnowHeightMap);
            SAMPLER(sampler_SnowHeightMap);

            float _HeightScale;
            float3 _SnowWorldCenter;
            float2 _SnowWorldSize;

            float2 WorldToSnowUV(float3 worldPos)
            {
                float2 localPos = float2(worldPos.x - _SnowWorldCenter.x, worldPos.z - _SnowWorldCenter.z);
                return float2((localPos.x / _SnowWorldSize.x) + 0.5, (localPos.y / _SnowWorldSize.y) + 0.5);
            }

            Varyings DepthVert(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float2 snowUV = WorldToSnowUV(positionWS);
                float height = SAMPLE_TEXTURE2D_LOD(_SnowHeightMap, sampler_SnowHeightMap, snowUV, 0).r;

                float displacement = (1.0 - height) * _HeightScale;
                float3 displacedPos = input.positionOS.xyz - input.normalOS * displacement;

                output.positionCS = TransformObjectToHClip(displacedPos);
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
