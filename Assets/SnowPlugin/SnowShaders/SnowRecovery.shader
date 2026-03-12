// 雪地恢复Shader - 让雪慢慢恢复到原始状态
Shader "Hidden/SnowRecovery"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TargetHeightMap ("Target Height Map", 2D) = "white" {}
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
            sampler2D _TargetHeightMap;
            float _RecoverySpeed;  // 每秒恢复速度
            float _DeltaTime;
            float _EdgeFalloff;    // 边缘过渡宽度
            float _EdgeSmoothness; // 边缘过渡平滑度

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float currentHeight = tex2D(_MainTex, i.uv).r;
                float targetHeight = tex2D(_TargetHeightMap, i.uv).r; // 从初始高度图读取目标高度

                // 计算是否在边缘区域
                float2 uvCenter = i.uv - 0.5; // 中心为(0.5, 0.5)
                float distX = abs(uvCenter.x) * 2.0; // 0 (中心) 到 1 (边缘)
                float distY = abs(uvCenter.y) * 2.0;
                float distToEdge = max(distX, distY);

                // 计算边缘因子（0=中心可恢复，1=边缘不恢复）
                // 使用简单线性插值代替smoothstep，避免8位精度下的量化误差
                float edgeStart = 1.0 - _EdgeFalloff;

                float edgeFactor;
                if (distToEdge < edgeStart)
                {
                    edgeFactor = 0.0; // 内部区域，完全恢复
                }
                else if (distToEdge >= 1.0)
                {
                    edgeFactor = 1.0; // 完全边缘，不恢复
                }
                else
                {
                    // 简单线性插值
                    edgeFactor = (distToEdge - edgeStart) / _EdgeFalloff;
                }

                // 只在中心区域恢复（边缘保持不变）
                // 使用目标高度图的值作为恢复目标，而不是固定的1.0
                float recoveryAmount = _RecoverySpeed * _DeltaTime;
                float newHeight = min(targetHeight, currentHeight + recoveryAmount);

                // 根据edgeFactor混合：边缘保持原值，中心恢复
                newHeight = lerp(newHeight, currentHeight, edgeFactor);

                return float4(newHeight, newHeight, newHeight, 1.0);
            }
            ENDCG
        }
    }
}
