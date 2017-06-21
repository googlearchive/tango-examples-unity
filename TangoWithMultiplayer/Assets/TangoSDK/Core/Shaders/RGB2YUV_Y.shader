// Don't remove the following line. It is used to bypass Unity
// upgrader change. This is necessary to make sure the shader 
// continues to compile on Unity 5.2
// UNITY_SHADER_NO_UPGRADE
Shader "Hidden/Tango/RGB2YUV_Y"
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
            
            float RGB2Y(float4 col)
            {
                return ( 0.29882 * col.r + 0.58681 * col.g + 0.11436 * col.b);
            }
            
            sampler2D _MainTex;
            float _TexWidth;

            fixed4 frag (v2f i) : SV_Target
            {
                float halfTexelWidth = 0.5 / _TexWidth;
                
                float4 col1 = tex2D(_MainTex, i.uv + float2(halfTexelWidth * -3, 0));
                float4 col2 = tex2D(_MainTex, i.uv + float2(halfTexelWidth * -1, 0));
                float4 col3 = tex2D(_MainTex, i.uv + float2(halfTexelWidth * 1, 0));
                float4 col4 = tex2D(_MainTex, i.uv + float2(halfTexelWidth * 3, 0));
                
                return float4(RGB2Y(col1), RGB2Y(col2), RGB2Y(col3), RGB2Y(col4));
            }
            ENDCG
        }
    }
}
