Shader "Tango/Environmental Lighting/Bumped Specular" {
Properties {
    _Shininess ("Shininess", Range (0.03, 1)) = 0.078125
    _MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
    _BumpMap ("Normalmap", 2D) = "bump" {}
}
SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 250

CGPROGRAM
#include "TangoEnvironmentalLighting.cginc"
#pragma surface surf TangoEnvironmentLightingBumpedSpecular exclude_path:prepass nolightmap noforwardadd halfasview interpolateview

inline fixed4 LightingTangoEnvironmentLightingBumpedSpecular(
        SurfaceOutput s, fixed3 lightDir, fixed3 halfDir, fixed atten)
{
    fixed diff = max (0, dot (s.Normal, lightDir));
    fixed nh = max (0, dot (s.Normal, halfDir));
    fixed spec = pow (nh, s.Specular * 128) * s.Gloss;

    fixed4 c;
    c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec)
            * atten;
    UNITY_OPAQUE_ALPHA(c.a);
    c.rgb = s.Albedo * EnvironmentalLightingSpecular(reflect(halfDir, s.Normal));
    return c;
}

sampler2D _MainTex;
sampler2D _BumpMap;
half _Shininess;

struct Input {
    float2 uv_MainTex;
};

void surf(Input IN, inout SurfaceOutput o) {
    fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
    o.Albedo = tex.rgb;
    o.Gloss = tex.a;
    o.Alpha = tex.a;
    o.Specular = _Shininess;
    o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
}
ENDCG
}

FallBack "Mobile/VertexLit"
}
