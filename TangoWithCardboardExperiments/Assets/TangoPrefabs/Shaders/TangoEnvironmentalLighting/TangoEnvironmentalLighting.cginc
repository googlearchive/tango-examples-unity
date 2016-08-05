#ifndef TANGO_ENVIRONMENTAL_LIGHTING_INCLUDED
#define TANGO_ENVIRONMENTAL_LIGHTING_INCLUDED

// The Y, U, V texture.
// However, at present U and V textures are interleaved into the same texture,
// so we'll only sample from _TangoLightingYTex and _TangoLightingUTex.
sampler2D _TangoLightingYTex;
sampler2D _TangoLightingUTex;

// Width of the RGBA texture, this is for indexing the channel of color, not
// for scaling.
float _TangoLightingTexWidth;
float _TangoLightingTexHeight;

// Image based lighting diffuse map and exposure.
float4x4 _TangoLightingSphericalHarmonicMatrixR;
float4x4 _TangoLightingSphericalHarmonicMatrixG;
float4x4 _TangoLightingSphericalHarmonicMatrixB;
float _TangoLightingExposure;

// The camera orientation.
int _TangoLightingColorCameraRotation;

// Compute a modulo b.
float CustomMod(float x, float y)
{
    return x - (y * floor(x / y));
}

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

float3 GetRGBfromYUV(float x, float y) {
    // In this example, we are using HAL_PIXEL_FORMAT_YCrCb_420_SP format
    // the data packing is: texture_y will pack 1280x720 pixels into
    // a 320x720 RGBA8888 texture.
    // texture_Cb and texture_Cr will contain copies of the 2x2 downsampled
    // interleaved UV planes packed similarly.
    float y_value, u_value, v_value;

    float texel_x = x * _TangoLightingTexWidth;

    // Compute packed-pixel offset for Y value.
    float packed_offset = floor(CustomMod(texel_x, 4.0));

    // Avoid floating point precision problems: Make sure we're sampling from the
    // same pixel as we've computed packed_offset for.
    x = (floor(texel_x) + 0.5) / _TangoLightingTexWidth;

    float4 packed_y = tex2D(_TangoLightingYTex, float2(x, (1.0 - y)));
    if (packed_offset == 0)
    {
        y_value = packed_y.r;
    }
    else if (packed_offset == 1)
    {
        y_value = packed_y.g;
    }
    else if (packed_offset == 2)
    {
        y_value = packed_y.b;
    }
    else
    {
        y_value = packed_y.a;
    }

    float4 packed_uv = tex2D(_TangoLightingUTex, float2(x, (1.0 - y)));

    if (packed_offset == 0 || packed_offset == 1)
    {
        v_value = packed_uv.r;
        u_value = packed_uv.g;
    }
    else
    {
        v_value = packed_uv.b;
        u_value = packed_uv.a;
    }

    // The YUV to RBA conversion, please refer to: http://en.wikipedia.org/wiki/YUV
    // Y'UV420sp (NV21) to RGB conversion (Android) section.
    float3 color;
    color.r = y_value + 1.370705 * (v_value - 0.5);
    color.g = y_value - 0.698001 * (v_value - 0.5) - (0.337633 * (u_value - 0.5));
    color.b = y_value + 1.732446 * (u_value - 0.5);
    return color;
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
    if (_TangoLightingColorCameraRotation == 3)
    {
        float temp = u;
        u = 1.0 - v;
        v = temp;
    }
    else if (_TangoLightingColorCameraRotation == 2)
    {
        u = 1.0 - u;
        v = 1.0 - v;
    }
    else if (_TangoLightingColorCameraRotation == 1)
    {
        float temp = u;
        u = v;
        v = 1.0 - temp;
    }
    half3 color = GetRGBfromYUV(u, v);
    return Safepow3(_TangoLightingExposure * color, 0.454545);
}

#endif // TANGO_ENVIRONMENTAL_LIGHTING_INCLUDED
