// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "MyMobile/rj/Dissolution_Add"
{
    Properties 
    {
        _TintColor ("Diffuse Color", Color) = (0.6985294,0.6985294,0.6985294,1)
        _MainTex ("Diffuse Texture", 2D) = "white" {}
        _N_mask ("N_mask", Float ) = 0.3
        _MaskTexture ("Mask Texture", 2D) = "white" {}
        _C_BYcolor ("C_BYcolor", Color) = (1,0,0,1)
        _N_BY_QD ("N_BY_QD", Float ) = 3
        _N_BY_KD ("N_BY_KD", Float ) = 0.01
        // [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader 
    {
        Tags
        {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        Pass 
        {
            // Name "FORWARD"
            // Tags 
            // {
            //     "LightMode"="ForwardBase"
            // }
            
            Blend One One
            ZWrite Off
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            
            uniform sampler2D _MaskTexture; 
            uniform float4 _MaskTexture_ST;
            uniform sampler2D _MainTex; 
            uniform float4 _MainTex_ST;
            uniform float4 _TintColor;
            uniform float _N_mask;
            uniform float _N_BY_KD;
            uniform float4 _C_BYcolor;
            uniform float _N_BY_QD;
            
            struct VertexInput
            {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            
            struct VertexOutput 
            {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            
            VertexOutput vert (VertexInput v) 
            {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.pos = UnityObjectToClipPos(v.vertex );
                return o;
            }
            
            float4 frag(VertexOutput i) : COLOR 
            {
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex));
                float node_654 = (i.vertexColor.a*_N_mask);
                float4 _MaskTexture_var = tex2D(_MaskTexture,TRANSFORM_TEX(i.uv0, _MaskTexture));
                float node_5746_if_leA = step(node_654,_MaskTexture_var.r);
                float node_5746_if_leB = step(_MaskTexture_var.r,node_654);
                float node_5746 = lerp(node_5746_if_leB, 1.0, node_5746_if_leA*node_5746_if_leB);
                float node_5217_if_leA = step(node_654,(_MaskTexture_var.r+_N_BY_KD));
                float node_5217_if_leB = step((_MaskTexture_var.r+_N_BY_KD),node_654);
                float node_1274 = (node_5746-lerp(node_5217_if_leB, 1.0, node_5217_if_leA*node_5217_if_leB));
                float node_6450 = (node_5746+node_1274);
                float3 node_7666 = ((node_1274*_C_BYcolor.rgb)*_N_BY_QD);
                float3 emissive = (_TintColor.a*(((_TintColor.rgb*_MainTex_var.rgb)*node_6450)+node_7666));
                float3 finalColor = emissive + (_TintColor.a*node_7666);
                float node_6644 = (_TintColor.a*(_MainTex_var.a*node_6450));
                return fixed4(finalColor,node_6644);
            }
            ENDCG
        }
    }
    FallBack "Mobile/Particles/Additive"
}
