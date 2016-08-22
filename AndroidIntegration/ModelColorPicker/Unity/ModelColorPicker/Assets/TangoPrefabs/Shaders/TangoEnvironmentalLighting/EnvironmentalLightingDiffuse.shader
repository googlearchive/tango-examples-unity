Shader "Tango/Environmental Lighting/Diffuse" {
Properties {
    _MainTex ("Base (RGB)", 2D) = "white" {}
}
SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 150

CGPROGRAM
#include "TangoEnvironmentalLighting.cginc"
#pragma surface surf TangoEnvironmentLightingDiffuse noforwardadd

sampler2D _MainTex;

struct Input {
    float2 uv_MainTex;
};

fixed4 LightingTangoEnvironmentLightingDiffuse(SurfaceOutput s, UnityGI gi)
{
    gi.indirect.diffuse = EnvironmentalLightingDiffuse(s.Normal);
    fixed4 c = LightingLambert(s, gi);
    return c;
}

inline void LightingTangoEnvironmentLightingDiffuse_GI(
        SurfaceOutput s, UnityGIInput data, inout UnityGI gi)
{
    LightingLambert_GI(s, data, gi);
}

void surf(Input IN, inout SurfaceOutput o) {
    fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
    o.Albedo = c.rgb;
    o.Alpha = c.a;
}

ENDCG
}

FallBack "Mobile/VertexLit"
}
