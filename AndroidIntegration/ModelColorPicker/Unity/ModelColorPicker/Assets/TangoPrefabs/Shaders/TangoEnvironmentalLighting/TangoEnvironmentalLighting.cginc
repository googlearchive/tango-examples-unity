#ifndef TANGO_ENVIRONMENTAL_LIGHTING_INCLUDED
#define TANGO_ENVIRONMENTAL_LIGHTING_INCLUDED

// The environment map used for specular reflections.
sampler2D _TangoLightingEnvironmentMap;

// Image based lighting diffuse map and exposure.
float4x4 _TangoLightingSphericalHarmonicMatrixR;
float4x4 _TangoLightingSphericalHarmonicMatrixG;
float4x4 _TangoLightingSphericalHarmonicMatrixB;
float _TangoLightingExposure;

half Safepow(half base, half exponent) {
    return pow(max(0.0, base), exponent);
}

half3 Safepow3(half3 base, half exponent) {
    half3 result;
    result.x = Safepow(base.x, exponent);
    result.y = Safepow(base.y, exponent);
    result.z = Safepow(base.z, exponent);
    return result;
}

half3 EnvironmentalLightingDiffuse (half3 normal) {
    normal = mul((half3x3)UNITY_MATRIX_V, normal);
    half4 h_normal;
    h_normal.xyz = normal;
    h_normal.w = 1;
    half4 Mn_r = mul(_TangoLightingSphericalHarmonicMatrixR, h_normal);
    half4 Mn_g = mul(_TangoLightingSphericalHarmonicMatrixG, h_normal);
    half4 Mn_b = mul(_TangoLightingSphericalHarmonicMatrixB, h_normal);
    half3 color;
    color.r = dot(Mn_r, h_normal);
    color.g = dot(Mn_g, h_normal);
    color.b = dot(Mn_b, h_normal);
    return Safepow3(_TangoLightingExposure * color, 0.454545);
}

half3 EnvironmentalLightingSpecular (half3 refl) {
    refl = mul((half3x3)UNITY_MATRIX_V, refl);
    float c = sqrt(refl.x * refl.x + refl.y * refl.y);
    float k = c / sin(c);
    float lambda = atan((refl.x * sin(c)) / (c * cos(c)));
    float phi = asin(refl.y * sin(c) / c);
    float x = k * cos(phi) * sin(lambda);
    float y = k * sin(phi);
    float u = 0.5 - 0.5 * x;
    float v = 0.5 - 0.5 * y;
    half3 color = tex2D(_TangoLightingEnvironmentMap, float2(u, v));
    return Safepow3(_TangoLightingExposure * color, 0.454545);
}

#endif // TANGO_ENVIRONMENTAL_LIGHTING_INCLUDED
