// Don't remove the following line. It is used to bypass Unity
// upgrader change. This is necessary to make sure the shader 
// continues to compile on Unity 5.2
// UNITY_SHADER_NO_UPGRADE
Shader "Hidden/Tango/DepthEmulation"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
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
                float clipSpaceZ : TEXCOORD0;
            };
            
            float4x4 _ModelToTangoDepthCameraSpace;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
                o.clipSpaceZ = o.vertex.z;
                return o;
            }
            
            float4 frag (v2f i) : SV_Target
            {
                float biasedUnprojectedDepth = i.clipSpaceZ + 128;
                
                float isNegative = 0;
                if(biasedUnprojectedDepth < 0)
                {
                    biasedUnprojectedDepth = biasedUnprojectedDepth * -1;
                    isNegative = 1;
                }
                
                float integerPart;
                float fractionPart = modf(biasedUnprojectedDepth, integerPart);
                
                integerPart = saturate(integerPart / 255.0);
                
                //store integer part in red, and fraction in green. 
                return float4(integerPart, fractionPart, 0, 1);
            }
            ENDCG
        }
    }
}
