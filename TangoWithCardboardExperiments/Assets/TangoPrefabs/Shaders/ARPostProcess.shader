// Don't remove the following line. It is used to bypass Unity
// upgrader change. This is necessary to make sure the shader 
// continues to compile on Unity 5.2
// UNITY_SHADER_NO_UPGRADE
Shader "Tango/ARPostProcess" {
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Width   ("Width", Float) = 1280.0
        _Height  ("Height", Float) = 720.0
        _Fx      ("Fx", Float) = 1043.130005
        _Fy      ("Fy", Float) = 1038.619995
        _Cx      ("Cx", Float) = 641.926025
        _Cy      ("Cy", Float) = 359.115997
        _K0      ("K0", Float) = 0.246231
        _K1      ("K1", Float) = -0.727204
        _K2      ("K2", Float) = 0.726065
    }

    SubShader 
    {
        // These flags are required for this to work as a postprocess shader.
        Cull Off ZWrite Off ZTest Always

        Pass 
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

            v2f vert(appdata v)
            {
                v2f o;
                o.uv = v.uv;
                o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
                return o;
            }

            sampler2D _MainTex;

            float _Width;
            float _Height;
            float _Fx;
            float _Fy;
            float _Cx;
            float _Cy;
            float _K0;
            float _K1;
            float _K2;

            fixed4 frag(v2f i) : SV_Target
            {
                float4 normalized_coords;
                normalized_coords.x = (i.uv.x * _Width - _Cx) / _Fx;
                normalized_coords.y = (i.uv.y * _Height - _Cy) / _Fy;

                float r_u2 = normalized_coords.x * normalized_coords.x +
                             normalized_coords.y * normalized_coords.y;
                float4 normalized_distorted_coords;
                normalized_distorted_coords.x =
                    normalized_coords.x / (1.0 + r_u2 * (_K0 + r_u2 * (_K1 + r_u2 * _K2)));
                normalized_distorted_coords.y =
                    normalized_coords.y / (1.0 + r_u2 * (_K0 + r_u2 * (_K1 + r_u2 * _K2)));

                float4 distorted_coords;
                distorted_coords.x = normalized_distorted_coords.x * _Fx + _Cx;
                distorted_coords.y = normalized_distorted_coords.y * _Fy + _Cy;

                distorted_coords.x = distorted_coords.x / _Width;
                distorted_coords.y = distorted_coords.y / _Height;

                fixed4 col = tex2D(_MainTex, float2(distorted_coords.x,
                                   distorted_coords.y));

                return col;
            }
            ENDCG
        }
    }
}