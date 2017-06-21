// Don't remove the following line. It is used to bypass Unity
// upgrader change. This is necessary to make sure the shader 
// continues to compile on Unity 5.2
// UNITY_SHADER_NO_UPGRADE
Shader "Hidden/Tango/ColorCameraEmulation"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color (RGB)", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma multi_compile _ LIT_TANGO_AR_EMULATION
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.normal = v.normal;
                return o;
            }
            
            fixed4 _Color;
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Common sources of color
                fixed4 col = tex2D(_MainTex, i.uv) * i.color * _Color;
                
                #ifdef LIT_TANGO_AR_EMULATION
                // Simple and terrible lighting, with two fixed-direction diffuse 
                // 'lights' and a bit of ambient.
                col *= (0.65 * max(dot(i.normal, float3(0.291, 0.823, 0.4875)), 0))
                        + (0.15 * max(dot(i.normal, float3(-0.739, -0.5066, -0.444)), 0))
                        + 0.10;
                #endif
                
                return col;
            }
            ENDCG
        }
    }
}
