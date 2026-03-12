Shader "Custom/SnowDebugTest"
{
    Properties
    {
        _SnowHeightMap ("Snow Height Map", 2D) = "white" {}
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
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            TEXTURE2D(_SnowHeightMap);
            SAMPLER(sampler_SnowHeightMap);

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
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 直接显示HeightMap的值
                float2 snowUV = WorldToSnowUV(input.positionWS);
                float height = SAMPLE_TEXTURE2D(_SnowHeightMap, sampler_SnowHeightMap, snowUV).r;

                // 白色=未踩(1.0), 黑色=踩了(0.0)
                // 为了更明显，我们反转颜色：踩了的地方显示红色
                half3 color = height > 0.5 ? half3(1, 1, 1) : half3(1, 0, 0);

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
