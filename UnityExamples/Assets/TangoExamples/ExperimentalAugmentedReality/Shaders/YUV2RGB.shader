    Shader "Tango/YUV2RGB" {
    Properties 
    {
          _YTex ("Y channel texture", 2D) = "white" {}
          _UTex ("U channel texture", 2D) = "white" {}
          _VTex ("V channel texture", 2D) = "white" {}
          _TexWidth ("texture width", Float) = 1280.0
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
            
            // The Y, U, V texture.
            uniform sampler2D _YTex;
            uniform sampler2D _UTex;
            uniform sampler2D _VTex;
            
            // Width of the RGBA texture, this is for indexing the channel of color, not
            // for scaling.
            uniform float _TexWidth;
            
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
            // Compute a modulo b.
            float mod(float x, float y)
            {
                return x - y * floor(x / y);
            }
            
            void main()
            {
                // In this example, we are using HAL_PIXEL_FORMAT_YCrCb_420_SP format
                // the data packing is: texture_y will pack 1280x720 pixels into
                // a 320x720 RGBA8888 texture.
                // texture_Cb and texture_Cr will contain copies of the 2x2 downsampled
                // interleaved UV planes packed similarly.
                float y_value, u_value, v_value;

                float texel_x = textureCoordinates.s * _TexWidth;

                // Compute the Y value.
                int packed_offset = int(mod(texel_x, 4.0));
                
                vec4 packed_y = texture2D(_YTex, vec2(textureCoordinates.s, (1.0 - textureCoordinates.t)));
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

                vec4 packed_uv = texture2D(_UTex, vec2(textureCoordinates.s, (1.0 - textureCoordinates.t)));

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