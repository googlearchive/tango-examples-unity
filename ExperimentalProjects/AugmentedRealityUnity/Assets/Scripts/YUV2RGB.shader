Shader "Tango/YUV2RGB" {
    Properties 
    {
          _YTex ("Y channel texture", 2D) = "white" {}
          _UTex ("U channel texture", 2D) = "white" {}
          _VTex ("V channel texture", 2D) = "white" {}
    }
    SubShader 
    {
        // Setting the z write off to make sure our video overlay is always rendered at back.
        ZWrite Off
        Pass 
        {
            GLSLPROGRAM
            
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
            // Compute a modulo b.
            int mod(int a, int b) {
                return a - ((a / b) * b);
            }
            
            void main()
            {
                // In this example, we are using HAL_PIXEL_FORMAT_YCrCb_420_SP format
                // the data packing is: texture_y will pack 1280x720 pixels into
                // a 320x720 RGBA8888 texture.
                // texture_Cb and texture_Cr will contain copies of the 2x2 downsampled
                // interleaved UV planes packed similarly.
                float y_value, u_value, v_value;
                // Computing index in the color texture space (expected result). The target
                // texture size is 1280 x 720.
                int x = int(textureCoordinates.s * 1280.0);
                int y = int((1.0 - textureCoordinates.t) * 720.0);
                
                // Compute the Y value.
                int x_y_image = int(x / 4);
                int x_y_offset = mod(x, 4);
                int y_y_image = y;
                
                vec4 c_y = texture2D(_YTex, vec2(float(x_y_image) / float(320.0), float(y_y_image) / float(720.0)));
                if (x_y_offset == 0) {
                    y_value = c_y.r;
                } else if (x_y_offset == 1) {
                    y_value = c_y.g;
                } else if (x_y_offset == 2) {
                    y_value = c_y.b;
                } else if (x_y_offset == 3) {
                    y_value = c_y.a;
                }
                
                // Compute the U,V value.
                int x_uv_image = int(x / 4);
                int x_uv_offset = mod(x, 4);
                int y_uv_image = int(y / 2);

                vec4 c_uv = texture2D(_UTex, vec2(float(x_uv_image) / float(320.0), float(y_uv_image) / float(360.0)));

                if (x_uv_offset == 0 || x_uv_offset == 1) {
                    v_value = c_uv.r;
                    u_value = c_uv.g;
                } else  if (x_uv_offset == 2 || x_uv_offset == 3) {
                    v_value = c_uv.b;
                    u_value = c_uv.a;
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