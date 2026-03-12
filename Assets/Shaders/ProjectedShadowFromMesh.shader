Shader "Custom/ProjectedShadowFromMesh"
{
    Properties
    {
        [Header(Shadow Appearance)]
        _ShadowColor ("Shadow Color (阴影颜色)", Color) = (0, 0, 0, 0.6)
        _ShadowIntensity ("Intensity (强度)", Range(0, 1)) = 0.6

        [Header(Light Settings)]
        _LightPosX ("Light X (光源X)", Float) = 0
        _LightPosY ("Light Y (光源Y)", Float) = 10
        _LightPosZ ("Light Z (光源Z)", Float) = 0
        _ShadowPlaneY ("Shadow Plane Y (阴影平面高度)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent-1"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "ProjectedShadowPass"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            Stencil
            {
                Ref 1
                Comp NotEqual
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ShadowColor;
                half _ShadowIntensity;
                float _LightPosX;
                float _LightPosY;
                float _LightPosZ;
                float _ShadowPlaneY;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output;

                // 获取顶点的世界空间位置
                float3 worldVertexPos = TransformObjectToWorld(input.positionOS.xyz);

                // 点光源位置
                float3 lightPos = float3(_LightPosX, _LightPosY, _LightPosZ);

                // 从光源指向顶点的方向
                float3 lightDir = worldVertexPos - lightPos;

                // 计算投影位置
                float3 projectedPos;

                // 防止除以零
                if (abs(lightDir.y) > 0.0001)
                {
                    float t = (_ShadowPlaneY - lightPos.y) / lightDir.y;

                    // 只有当t > 0时才是有效投影
                    if (t > 0.0)
                    {
                        projectedPos = lightPos + lightDir * t;
                        projectedPos.y += 0.01; // 稍微抬高避免Z-fighting
                    }
                    else
                    {
                        // 顶点在光源上方，隐藏
                        projectedPos = float3(0, _ShadowPlaneY - 1000.0, 0);
                    }
                }
                else
                {
                    // 光线平行于地面，隐藏
                    projectedPos = float3(0, _ShadowPlaneY - 1000.0, 0);
                }

                // 转换到裁剪空间
                output.positionCS = TransformWorldToHClip(projectedPos);

                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                half4 color = _ShadowColor;
                color.a *= _ShadowIntensity;
                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
