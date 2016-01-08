Shader "Tango/YUV2RGB" {
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
        GLSLPROGRAM
        
        #pragma multi_compile __ DISTORTION_ON

        // The Y, U, V texture.
        uniform sampler2D _YTex;
        uniform sampler2D _UTex;
        uniform sampler2D _VTex;

        varying vec4 textureCoordinates; 

        #ifdef VERTEX
        void main()
        {
            // We don't apply any projection or view matrix here to make sure that
            // the geometry is rendered in the screen space.
            textureCoordinates = gl_MultiTexCoord0;
            gl_Position = gl_Vertex;
        }
        
        #endif

        #ifdef FRAGMENT
        // Width of the RGBA texture, this is for indexing the channel of color, not
        // for scaling.
        uniform float _TexWidth;
        uniform float _TexHeight;
        uniform float _Fx;
        uniform float _Fy;
        uniform float _Cx;
        uniform float _Cy;
        uniform float _K0;
        uniform float _K1;
        uniform float _K2;

        // Compute a modulo b.
        float custom_mod(float x, float y)
        {
            return x - y * floor(x / y);
        }
        
        void main()
        {
            float undistored_x = textureCoordinates.s;
            float undistored_y = textureCoordinates.t;
            float x = textureCoordinates.s;
            float y = textureCoordinates.t;

            #ifdef DISTORTION_ON
            x = (x * _TexWidth - _Cx) / _Fx;
            y = (y * _TexHeight - _Cy) / _Fy;

            float r2 = x * x + y * y;
            float denom = 1.0 + r2 * (_K0 + r2 * (_K1 + r2 * _K2));
            float icdist = 1.0 / denom;
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

            // Compute the Y value.
            int packed_offset = int(custom_mod(texel_x, 4.0));
            
            vec4 packed_y = texture2D(_YTex, vec2(undistored_x, (1.0 - undistored_y)));
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
            else if (packed_offset == 3)
            {
                y_value = packed_y.a;
            }

            vec4 packed_uv = texture2D(_UTex, vec2(undistored_x, (1.0 - undistored_y)));

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

            gl_FragColor = vec4(r, g, b, 1.0);
        }
        #endif

        ENDGLSL
    }
}
}