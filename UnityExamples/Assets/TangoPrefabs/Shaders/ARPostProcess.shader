Shader "Tango/ARPostProcess" {
Properties 
{
  _MainTex ("Main Texture", 2D) = "white" {}
  _Width   ("Width", Float) = 1280.0
  _Height  ("Height", Float) = 720.0
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
Pass 
{
    GLSLPROGRAM
    uniform sampler2D _MainTex;
    
    varying vec4 textureCoordinates; 

    #ifdef VERTEX
    void main()
    {
        textureCoordinates = gl_MultiTexCoord0;
        gl_Position = gl_ModelViewProjectionMatrix  * gl_Vertex;
        gl_Position = vec4(gl_Position.x * 1.03, gl_Position.y * 1.03, 0, 1);
    }
    
    #endif

    #ifdef FRAGMENT
    uniform float _Width;
    uniform float _Height;
    uniform float _Fx;
    uniform float _Fy;
    uniform float _Cx;
    uniform float _Cy;
    uniform float _K0;
    uniform float _K1;
    uniform float _K2;

    void main()
    {
        vec4 normalized_coords;
        normalized_coords.s = (textureCoordinates.s * _Width - _Cx) / _Fx;
        normalized_coords.t = (textureCoordinates.t * _Height - _Cy) / _Fy;
        
        float r_u2 = normalized_coords.s * normalized_coords.s +
                normalized_coords.t * normalized_coords.t;
        vec4 normalized_distorted_coords;
        normalized_distorted_coords.s =
            normalized_coords.s * (1.0 + r_u2 * (_K0 + r_u2 * (_K1 + r_u2 * _K2)));
        normalized_distorted_coords.t =
            normalized_coords.t * (1.0 + r_u2 * (_K0 + r_u2 * (_K1 + r_u2 * _K2)));
        
        vec4 distorted_coords;
        distorted_coords.s = normalized_distorted_coords.s * _Fx + _Cx;
        distorted_coords.t = normalized_distorted_coords.t * _Fy + _Cy;
        
        distorted_coords.s = distorted_coords.s / _Width;
        distorted_coords.t = distorted_coords.t / _Height;
        

        
        gl_FragColor = texture2D(_MainTex, vec2(distorted_coords.s,
            distorted_coords.t));
            
        if (distorted_coords.s < 0.0 || distorted_coords.s > 1.0 ||
            distorted_coords.t < 0.0 || distorted_coords.t > 1.0) {
            gl_FragColor = vec4(1.0, 0.0, 0.0, 1.0);
            return;
        }
    }
    #endif

    ENDGLSL
}
}
}