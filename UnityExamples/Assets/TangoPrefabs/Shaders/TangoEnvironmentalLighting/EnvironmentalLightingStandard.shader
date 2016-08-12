Shader "Tango/Environmental Lighting/Standard" {
    Properties {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _Parallax ("Height Scale", Range (0.005, 0.08)) = 0.02
        _ParallaxMap ("Height Map", 2D) = "black" {}

        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        _DetailMask("Detail Mask", 2D) = "white" {}

        _DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
        _DetailNormalMapScale("Scale", Float) = 1.0
        _DetailNormalMap("Normal Map", 2D) = "bump" {}

        [Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0


        // Blending state
        [HideInInspector] _Mode ("__mode", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #include "UnityPBSLighting.cginc"
        #include "TangoEnvironmentalLighting.cginc"

        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf TangoEnvironmentLightingStandard fullforwardshadows

        #pragma target 3.0

        // Properties.
        float4 _Color;
        sampler2D _MainTex;
        float _Cutoff;
        float _Glossiness;
        float _Metallic;
        sampler2D _MetallicGlossMap;
        float _OcclusionStrength;
        sampler2D _OcclusionMap;
        float4 _EmissionColor;
        sampler2D _EmissionMap;

        struct Input {
            float2 uv_MainTex;
        };

        half4 LightingTangoEnvironmentLightingStandard (SurfaceOutputStandard s, half3 viewDir, UnityGI gi) {
            s.Normal = normalize (s.Normal);

            half oneMinusReflectivity;
            half3 specColor;
            s.Albedo = DiffuseAndSpecularFromMetallic (s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

            // shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
            // this is necessary to handle transparency in physically correct way - only diffuse component gets affected by alpha
            half outputAlpha;
            s.Albedo = PreMultiplyAlpha (s.Albedo, s.Alpha, oneMinusReflectivity, /*out*/ outputAlpha);

            // Add image based lighting component.
            gi.indirect.diffuse = EnvironmentalLightingDiffuse(s.Normal) * (1 - s.Smoothness);
            gi.indirect.specular = EnvironmentalLightingSpecular(reflect(viewDir, s.Normal)) * s.Smoothness;

            half4 c = UNITY_BRDF_PBS(s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);
            c.rgb +=  UNITY_BRDF_GI(s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, s.Occlusion, gi);
            c.a = outputAlpha;
            return c;
        }

        void surf (Input IN, inout SurfaceOutputStandard o) {
            // Albedo comes from a texture tinted by color
            o.Albedo = (tex2D (_MainTex, IN.uv_MainTex) * _Color).rgb;
            o.Smoothness = _Glossiness;
            o.Metallic = (tex2D (_MetallicGlossMap, IN.uv_MainTex) * _Metallic).rgb;
            o.Occlusion = (tex2D (_OcclusionMap, IN.uv_MainTex) * _OcclusionStrength).rgb;
            o.Emission = (tex2D (_EmissionMap, IN.uv_MainTex) * _EmissionColor).rgb;
            o.Alpha = _Cutoff;
        }

        inline void LightingTangoEnvironmentLightingStandard_GI (SurfaceOutputStandard s,UnityGIInput data, inout UnityGI gi) {
            gi = UnityGlobalIllumination(data, s.Occlusion, s.Smoothness, s.Normal);
        }
        ENDCG
    }
    FallBack "Standard"
    CustomEditor "StandardShaderGUI"
}
