Shader "Custom/InteractiveSnow"
{
    Properties
    {
        [Header(Base Textures)]
        _BaseMap ("Albedo (RGB)", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Range(0, 2)) = 1.0

        [Header(Snow Properties)]
        _SnowColor ("Snow Color", Color) = (0.95, 0.95, 0.98, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.3
        _Metallic ("Metallic", Range(0, 1)) = 0.0

        [Header(Height Displacement)]
        _HeightScale ("Height Scale (Vertex)", Range(0, 0.5)) = 0.1
        _HeightMapStrength ("Height Map Strength", Range(0, 1)) = 1.0

        [Header(Parallax Occlusion Mapping)]
        _ParallaxScale ("Parallax Depth", Range(0, 0.1)) = 0.02
        _ParallaxSteps ("Parallax Steps", Range(4, 32)) = 16

        [Header(Snow Detail)]
        _SnowNormalMap ("Snow Normal Map", 2D) = "bump" {}
        _SnowNormalScale ("Snow Normal Scale", Range(0, 2)) = 1.0

        [HideInInspector] _SnowHeightMap ("Snow Height Map", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float3 viewDirTS : TEXCOORD4;
                float3 viewDirWS : TEXCOORD5;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_SnowNormalMap);
            SAMPLER(sampler_SnowNormalMap);
            TEXTURE2D(_SnowHeightMap);
            SAMPLER(sampler_SnowHeightMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _SnowColor;
                float _NormalScale;
                float _SnowNormalScale;
                float _Smoothness;
                float _Metallic;
                float _HeightScale;
                float _HeightMapStrength;
                float _ParallaxScale;
                float _ParallaxSteps;
            CBUFFER_END

            // Global snow parameters (set by SnowInteractionManager)
            float4x4 _WorldToUVMatrix;
            float3 _SnowWorldCenter;
            float2 _SnowWorldSize;

            // 世界坐标转换为雪地UV坐标
            float2 WorldToSnowUV(float3 worldPos)
            {
                float2 localPos = float2(
                    worldPos.x - _SnowWorldCenter.x,
                    worldPos.z - _SnowWorldCenter.z
                );

                float2 uv = float2(
                    (localPos.x / _SnowWorldSize.x) + 0.5,
                    (localPos.y / _SnowWorldSize.y) + 0.5
                );

                return uv;
            }

            // 简化的视差映射（优化用于移动端）
            float2 ParallaxMapping(float2 uv, float3 viewDirTS, float3 positionWS, out float parallaxHeight)
            {
                // 获取雪地高度
                float2 snowUV = WorldToSnowUV(positionWS);
                float height = SAMPLE_TEXTURE2D(_SnowHeightMap, sampler_SnowHeightMap, snowUV).r;

                // 简单视差偏移（不使用复杂的POM以提高性能）
                float2 parallaxOffset = viewDirTS.xy / viewDirTS.z * (1.0 - height) * _ParallaxScale;

                parallaxHeight = height;
                return uv - parallaxOffset;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                // 计算世界坐标（用于后续采样）
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                // 获取雪地高度值（LOD 0确保在顶点着色器中安全采样）
                float2 snowUV = WorldToSnowUV(positionWS);
                float heightValue = SAMPLE_TEXTURE2D_LOD(_SnowHeightMap, sampler_SnowHeightMap, snowUV, 0).r;

                // 顶点位移（基于高度图，向下沉）
                float displacement = (1.0 - heightValue) * _HeightScale * _HeightMapStrength;
                float3 displacedPosOS = input.positionOS.xyz - input.normalOS * displacement;

                // 标准变换
                VertexPositionInputs vertexInput = GetVertexPositionInputs(displacedPosOS);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = normalInput.normalWS;
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);

                // 计算切线空间的视图方向（用于视差映射）
                float3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.viewDirWS = viewDirWS;

                float3 bitangentWS = cross(normalInput.normalWS, normalInput.tangentWS) * input.tangentOS.w;
                float3x3 tangentToWorld = float3x3(normalInput.tangentWS, bitangentWS, normalInput.normalWS);
                output.viewDirTS = mul(tangentToWorld, viewDirWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 应用视差映射
                float parallaxHeight;
                float2 parallaxUV = ParallaxMapping(input.uv, normalize(input.viewDirTS), input.positionWS, parallaxHeight);

                // 采样基础纹理
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, parallaxUV) * _BaseColor;

                // 获取雪地高度（用于混合雪色）
                float2 snowUV = WorldToSnowUV(input.positionWS);
                float snowHeight = SAMPLE_TEXTURE2D(_SnowHeightMap, sampler_SnowHeightMap, snowUV).r;

                // 根据踩踏深度混合颜色（被踩深的地方显示底色）
                float snowMask = smoothstep(0.3, 0.8, snowHeight);
                half3 finalColor = lerp(albedo.rgb, _SnowColor.rgb, snowMask);

                // 采样法线
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, parallaxUV), _NormalScale);
                half3 snowNormalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_SnowNormalMap, sampler_SnowNormalMap, parallaxUV * 5.0), _SnowNormalScale);

                // 混合法线
                normalTS = lerp(normalTS, snowNormalTS, snowMask);

                // 切线空间转世界空间
                float3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 tangentToWorld = float3x3(input.tangentWS.xyz, bitangentWS, input.normalWS);
                float3 normalWS = normalize(mul(normalTS, tangentToWorld));

                // 光照计算
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.normalWS = normalWS;
                lightingInput.viewDirectionWS = normalize(input.viewDirWS);
                lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = finalColor;
                surfaceData.alpha = 1.0;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.occlusion = 1.0;
                surfaceData.emission = 0;

                half4 color = UniversalFragmentPBR(lightingInput, surfaceData);

                return color;
            }
            ENDHLSL
        }

        // ShadowCaster Pass（投影）
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

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
            };

            TEXTURE2D(_SnowHeightMap);
            SAMPLER(sampler_SnowHeightMap);

            float _HeightScale;
            float _HeightMapStrength;
            float3 _SnowWorldCenter;
            float2 _SnowWorldSize;

            float2 WorldToSnowUV(float3 worldPos)
            {
                float2 localPos = float2(worldPos.x - _SnowWorldCenter.x, worldPos.z - _SnowWorldCenter.z);
                return float2((localPos.x / _SnowWorldSize.x) + 0.5, (localPos.y / _SnowWorldSize.y) + 0.5);
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float2 snowUV = WorldToSnowUV(positionWS);
                float heightValue = SAMPLE_TEXTURE2D_LOD(_SnowHeightMap, sampler_SnowHeightMap, snowUV, 0).r;

                float displacement = (1.0 - heightValue) * _HeightScale * _HeightMapStrength;
                float3 displacedPosOS = input.positionOS.xyz - input.normalOS * displacement;

                output.positionCS = TransformObjectToHClip(displacedPosOS);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
