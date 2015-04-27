Shader "Tango/PointCloud" {
  SubShader {
     Pass {
        GLSLPROGRAM

        #ifdef VERTEX
        uniform mat4 local_transformation;
        varying vec4 v_color;
        // Used to invert y values of PointCloud to put it
        // in Unity Camera's Coordinate Frame.
        mat4 invert_y = mat4(1.0, 0.0, 0.0, 0.0,
                             0.0, -1.0, 0.0, 0.0,
                             0.0, 0.0, 1.0, 0.0,
                             0.0, 0.0, 0.0, 1.0);
        void main()
        {   
           gl_PointSize = 5.0;
           gl_Position = gl_ModelViewProjectionMatrix * invert_y * gl_Vertex;
           v_color = gl_Vertex;
        }
        #endif

        #ifdef FRAGMENT
        varying vec4 v_color;
        void main()
        {
           gl_FragColor = v_color;
        }
        #endif

        ENDGLSL
     }
  }
}
