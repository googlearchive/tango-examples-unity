Shader "Tango/PointCloud" {
Properties{
        point_size("Point Size", Float) = 5.0
}
  SubShader {
     Pass {
        GLSLPROGRAM
        
        #ifdef VERTEX
        uniform mat4 depthCameraTUnityWorld;
        uniform float point_size;
        varying vec4 v_color;
        void main()
        {   
           gl_PointSize = point_size;
           gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
           
           // Color should be based on pose relative info
           v_color = depthCameraTUnityWorld * gl_Vertex;
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
