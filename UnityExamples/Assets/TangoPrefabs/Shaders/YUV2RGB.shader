Shader "Tango/YUV2RGB"
{
Properties 
{
    _YTex ("Y channel texture", 2D) = "white" {}
    _UTex ("U channel texture", 2D) = "white" {}
    _VTex ("V channel texture", 2D) = "white" {}
    _TexWidth ("texture width", Float) = 1280.0
    _TexHeight ("texture height", Float) = 720.0
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
    // Setting the z write off to make sure our video overlay is always rendered at back.
    ZWrite Off
    ZTest Off
    Tags { "Queue" = "Background" }
    Pass 
    {
        CGPROGRAM
        #pragma multi_compile _ DISTORTION_ON
        
        #pragma vertex vert
        #pragma fragment frag

        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
            float2 uv : TEXCOORD0;
        };
        
        v2f vert (appdata v)
        {
            v2f o;
            // We don't apply any projection or view matrix here to make sure that
            // the geometry is rendered in the screen space.
            o.vertex = v.vertex;
            o.uv = v.uv;
            return o;
        }

        // The Y, U, V texture.
        // However, at present U and V textures are interleaved into the same texture,
        // so we'll only sample from _YTex and _UTex.
        sampler2D _YTex;
        sampler2D _UTex;
        
        // Width of the RGBA texture, this is for indexing the channel of color, not
        // for scaling.
        float _TexWidth;
        float _TexHeight;
        float _Fx;
        float _Fy;
        float _Cx;
        float _Cy;
        float _K0;
        float _K1;
        float _K2;
        
        // Compute a modulo b.
        float custom_mod(float x, float y)
        {
            return x - (y * floor(x / y));
        }
        
        fixed4 frag (v2f i) : SV_Target
        {
            float undistored_x = i.uv.x;
            float undistored_y = i.uv.y;
            float x = i.uv.x;
            float y = i.uv.y;

            #ifdef DISTORTION_ON
            x = (x * _TexWidth - _Cx) / _Fx;
            y = (y * _TexHeight - _Cy) / _Fy;

            float r2 = x * x + y * y;
            float icdist = 1.0 + r2 * (_K0 + r2 * (_K1 + r2 * _K2));
            undistored_x = x * icdist;
            undistored_y = y * icdist;

            undistored_x = (undistored_x * _Fx + _Cx) / _TexWidth;
            undistored_y = (undistored_y * _Fy + _Cy) / _TexHeight;
            #endif

            // In this example, we are using HAL_PIXEL_FORMAT_YCrCb_420_SP format
            // the data packing is: texture_y will pack 1280x720 pixels into
            // a 320x720 RGBA8888 texture.
            // texture_Cb and texture_Cr will contain copies of the 2x2 downsampled
            // interleaved UV planes packed similarly.
            float y_value, u_value, v_value;

            float texel_x = undistored_x * _TexWidth;

            // Compute packed-pixel offset for Y value.
            float packed_offset = floor(custom_mod(texel_x, 4.0));
            
            // Avoid floating point precision problems: Make sure we're sampling from the 
            // same pixel as we've computed packed_offset for. 
            undistored_x = (floor(texel_x) + 0.5) / _TexWidth;
            
            float4 packed_y = tex2D(_YTex, float2(undistored_x, (1.0 - undistored_y)));
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

            float4 packed_uv = tex2D(_UTex, float2(undistored_x, (1.0 - undistored_y)));

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
            float r = y_value + 1.370705 * (v_value - 0.5);
            float g = y_value - 0.698001 * (v_value - 0.5) - (0.337633 * (u_value - 0.5));
            float b = y_value + 1.732446 * (u_value - 0.5);

            return float4(r, g, b, 1.0);
        }
        ENDCG
    }
}
}