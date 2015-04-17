Shader "Tango/PointCloud" {
  SubShader {
     Pass {
        GLSLPROGRAM

        #ifdef VERTEX
        mat4 inverse_z = mat4(1.0, 0.0, 0.0, 0.0,
                              0.0, 1.0, 0.0, 0.0,
                              0.0, 0.0, -1.0, 0.0,
                              0.0, 0.0, 0.0, 1.0);
        uniform mat4 local_transformation;
        varying vec4 v_color;
        void main()
        {
           // Note that the model matrix of this geometry is identity, that make the gl_ModelViewProjectionMatrix
           // into projection_matrix * view_matrix. local_transformation here is serving as a model matrix essentially.
           gl_Position =  gl_ModelViewProjectionMatrix * local_transformation * inverse_z * gl_Vertex;
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
