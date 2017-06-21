// Don't remove the following line. It is used to bypass Unity
// upgrader change. This is necessary to make sure the shader 
// continues to compile on Unity 5.2
// UNITY_SHADER_NO_UPGRADE
Shader "Hidden/Tango/RGB2YUV_CbCr"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TexWidth ("Color camera texture width", Float) = 1280
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
                o.uv = v.uv;
                o.uv.y = 1 - o.uv.y;
                return o;
            }
            
            float2 RGB2CbCr(float4 col)
            {
                return float2( (-0.17249 * col.r) - (0.33872 * col.g) + (0.51121 * col.b) + 0.5,
                               (0.51155 * col.r) - (0.42811 * col.g) - (0.08343 * col.b) + 0.5);
            }
            
            sampler2D _MainTex;
            float _TexWidth;

            fixed4 frag (v2f i) : SV_Target
            {
                float halfTexelWidth = 0.5 / _TexWidth;
                
                float2 CbCr1 = RGB2CbCr(tex2D(_MainTex, i.uv + float2(-halfTexelWidth, 0)));
                float2 CbCr2 = RGB2CbCr(tex2D(_MainTex, i.uv + float2(halfTexelWidth, 0)));
                
                return float4(CbCr1.y, CbCr1.x, CbCr2.y, CbCr2.x);
            }
            ENDCG
        }
    }
}
