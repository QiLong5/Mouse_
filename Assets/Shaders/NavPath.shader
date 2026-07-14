Shader "Custom/NavPath"
{
    Properties
    {
        _MainTex("箭头纹理", 2D) = "white" {}
        _ScrollYSpeed("Y轴滚动速度", Range(-20, 20)) = 2
        _IconSpacing("图标间距(周期÷图标长, 1=紧贴无间距, 2=每个图标后空一个图标位)", Range(1, 10)) = 1

        [Toggle] _EnableFade("启用渐隐", Float) = 1
        _FadeDistance("渐隐范围", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        ZWrite Off
        Cull Off // 双面渲染
        Blend SrcAlpha OneMinusSrcAlpha // Alpha混合

        Pass
        {
            Name "NavPathPass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ENABLEFADE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _ScrollYSpeed;
                half _FadeDistance;
                half _IconSpacing;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 【调试禁用】禁用NavPath shader
            //    return half4(0, 1, 0, 1); // 绿色

                // UV滚动动画
                // 锚定到“目标端”而非“玩家端”：在原始映射 uv.y*_MainTex_ST.y 上再减去一个常量
                // _MainTex_ST.y(= totalLength/lineWidth），把纹理相位整体平移。
                // 平移后 along = (uv.y - 1)*tiling = -(距目标的世界距离)/lineWidth：
                //   · 只与“距目标距离”有关 ⇒ 目标静止时图标钉在世界中，玩家移动只从脚下消耗、不再整排前移；
                //   · 是“相位平移”而非“V 轴镜像”，三角形朝向与流动方向都与原版一致（不会上下颠倒）。
                // 时间滚动项保持原样，维持“朝目标流动”的指向动画。
                float along = input.uv.y * _MainTex_ST.y - _MainTex_ST.y + _ScrollYSpeed * _Time.y;

                // 图标间距：把“平铺周期”与“图标本身”解耦。周期 = _IconSpacing 个图标长（沿线以 lineWidth 为 1 单位）。
                // 每个周期只在前 1 段画图标(v=q 采样 0..1)，后 (_IconSpacing-1) 段留空 → 图标大小不变、彼此拉开间距。
                // _IconSpacing=1 时 period=1、q=frac(along)、始终 inGap=0 ⇒ 与旧版逐像素等价（默认不改观感）。
                float period = max(_IconSpacing, 1.0);
                float q = along - period * floor(along / period);   // 相位 mod 到 [0, period)
                float inGap = step(1.0, q);                          // q≥1 落在间隙 → 透明

                float scrolledU = input.uv.x * _MainTex_ST.x;
                float2 scrolledUV = frac(float2(scrolledU, q));
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, scrolledUV);
                col.a *= (1.0 - inGap);

                #ifdef _ENABLEFADE_ON
                    float fadeStart = 1.0 - _FadeDistance;
                    float fade = saturate((input.uv.y - fadeStart) / _FadeDistance);
                    col.a *= (1.0 - fade);
                #endif

                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
