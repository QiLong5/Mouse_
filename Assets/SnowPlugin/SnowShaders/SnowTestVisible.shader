Shader "Custom/SnowTestVisible"
{
    Properties
    {
        _HeightScale ("Height Scale", Range(0, 1)) = 0.2
        [HideInInspector] _SnowHeightMap ("Snow Height Map", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
            "DisableBatching"="True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float height : TEXCOORD1;
            };

            TEXTURE2D(_SnowHeightMap);
            SAMPLER(sampler_SnowHeightMap);

            float _HeightScale;
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

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float2 snowUV = WorldToSnowUV(positionWS);
                float height = SAMPLE_TEXTURE2D_LOD(_SnowHeightMap, sampler_SnowHeightMap, snowUV, 0).r;

                // 顶点下沉（增强效果）
                float displacement = (1.0 - height) * _HeightScale;
                float3 displacedPos = input.positionOS.xyz - input.normalOS * displacement;

                output.positionCS = TransformObjectToHClip(displacedPos);
                output.positionWS = TransformObjectToWorld(displacedPos);
                output.height = height;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 直接根据height显示颜色
                // 白色(1.0) = 未踩踏
                // 渐变到黑色(0.0) = 踩踏
                // 为了更明显，被踩的地方显示鲜艳颜色

                half3 color;
                if (input.height > 0.95)
                {
                    color = half3(1, 1, 1); // 白色 - 未踩踏
                }
                else if (input.height > 0.5)
                {
                    color = half3(1, 1, 0); // 黄色 - 轻微踩踏
                }
                else
                {
                    color = half3(1, 0, 0); // 红色 - 深度踩踏
                }

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
