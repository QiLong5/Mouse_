Shader "Custom/NavPath"
{
    Properties
    {
        _MainTex("箭头纹理", 2D) = "white" {}
        _ScrollYSpeed("Y轴滚动速度", Range(-20, 20)) = 2

        [Toggle] _EnableFade("启用渐隐", Float) = 1
        _FadeDistance("渐隐范围", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100

        ZWrite Off
        Cull Off // 双面渲染
        Blend SrcAlpha OneMinusSrcAlpha // Alpha混合

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ENABLEFADE_ON

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _ScrollYSpeed;
            fixed _FadeDistance;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // UV滚动动画
                fixed4 col = tex2D(_MainTex, frac(i.uv * _MainTex_ST.xy + float2(0, _ScrollYSpeed) * _Time));

                #ifdef _ENABLEFADE_ON
                    float fadeStart = 1.0 - _FadeDistance;
                    float fade = saturate((i.uv.y - fadeStart) / _FadeDistance);
                    col.a *= (1 - fade);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
