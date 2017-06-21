// Don't remove the following line. It is used to bypass Unity
// upgrader change. This is necessary to make sure the shader 
// continues to compile on Unity 5.2
// UNITY_SHADER_NO_UPGRADE
Shader "Hidden/Tango/RGB_ARScreen"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _UVBottomLeft ("Bottom Left UV coordinate", Vector) = (0, 0, 0, 0)
        _UVBottomRight ("Bottom Right UV coordinate", Vector) = (1, 0, 0, 0)
        _UVTopLeft ("Top Left UV coordinate", Vector) = (0, 1, 0, 0)
        _UVTopRight ("Top Right UV coordinate", Vector) = (1, 1, 0, 0)
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
            
            sampler2D _MainTex;
            float4 _UVBottomLeft;
            float4 _UVBottomRight;
            float4 _UVTopLeft;
            float4 _UVTopRight;

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
                if (v.uv.x < 0.5 && v.uv.y < 0.5)
                {
                    o.uv = _UVBottomLeft;
                }
                else if (v.uv.x > 0.5 && v.uv.y < 0.5)
                {
                    o.uv = _UVBottomRight;
                }
                else if (v.uv.x < 0.5 && v.uv.y > 0.5)
                {
                    o.uv = _UVTopLeft;
                }
                else
                {
                    o.uv = _UVTopRight;
                }
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // On Tango devices, the camera texture is flipped so that v=0 is the top and v=1 is the bottom.
                return tex2D(_MainTex, float2(i.uv.x, 1 - i.uv.y));
            }
            ENDCG
        }
    }
}
