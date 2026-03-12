// 踩踏绘制Shader - 将足迹绘制到RenderTexture
Shader "Hidden/SnowFootprintDraw"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
            float2 _FootprintPos;       // UV坐标 (0-1)
            float2 _FootprintRadius;    // UV空间半径
            float _FootprintDepth;      // 深度 (0-1)
            float _FootprintSoftness;   // 柔和度

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 当前高度值
                float currentHeight = tex2D(_MainTex, i.uv).r;

                // 计算到踩踏中心的距离
                float2 offset = (i.uv - _FootprintPos) / _FootprintRadius;
                float dist = length(offset);

                // 使用平滑圆形衰减
                float influence = 1.0 - smoothstep(0.0, _FootprintSoftness, dist);

                // 计算新的高度（减去踩踏深度）
                float targetHeight = currentHeight - _FootprintDepth * influence;
                float newHeight = max(0.0, targetHeight);

                return float4(newHeight, newHeight, newHeight, 1.0);
            }
            ENDCG
        }
    }
}
