// Don't remove the following line. It is used to bypass Unity
// upgrader change. This is necessary to make sure the shader 
// continues to compile on Unity 5.2
// UNITY_SHADER_NO_UPGRADE
Shader "Tango/PointCloud (Occlusion)" {
    SubShader {
        Tags { "Queue" = "Background-1" }
        Pass {
            ColorMask 0
        
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
               float4 vertex : POSITION;
            };
       
            struct v2f
            {
               float4 vertex : SV_POSITION;
               float size : PSIZE;
            };
           
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
                o.size = 30;
                return o;
            }
           
            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(1, 1, 1, 1);
            }
            ENDCG
        }
    }
}
