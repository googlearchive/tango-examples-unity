//-----------------------------------------------------------------------
// <copyright file="ZTestBlur.shader" company="Google">
//
// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------
Shader "Custom/ZTestBlur"
{
    Properties
    {
        _CameraDepthTexture ("Camera Depth Texture", 2D) = "white" {}
        _MainTex ("Main Texture", 2D) = "white" {}
        _OcclusionColor ("Occlusion Color", Color) = (1, 1, 1, 1)
        _BlurThresholdMax("Highlight Threshold Max", Range (0.001, 0.01)) = 0.01
        _RimPower ("Rim Power", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
 
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
 
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
 
            uniform sampler2D _CameraDepthTexture;
            uniform sampler2D _MainTex;
            uniform float4 _OcclusionColor;
            uniform float _BlurThresholdMax;
            uniform float _RimPower;
            
            float4 _MainTex_ST;
 
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 projPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 normal : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
            };
 
            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                o.projPos = ComputeScreenPos(o.pos);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.normal = normalize(v.normal);
                o.viewDir = normalize(ObjSpaceViewDir(v.vertex));
 
                return o;
            }
 
            half4 frag(v2f i) : COLOR
            {
                float4 finalColor = tex2D(_MainTex,i.uv);

                // Get the value in the depth texture.
                float depthZ = LinearEyeDepth(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)).r);

                // Get the distance z value from the camera.
                float cameraDistZ = i.projPos.z + (_ProjectionParams.y * 2);

                float sampleScale = 0;

                #define GRABPIXEL(weight,kernel) (LinearEyeDepth(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos + float4(kernel * _BlurThresholdMax, kernel * _BlurThresholdMax, 0, 0))).r) < cameraDistZ) * weight

                sampleScale += GRABPIXEL(0.05, -4.0);
                sampleScale += GRABPIXEL(0.09, -3.0);
                sampleScale += GRABPIXEL(0.12, -2.0);
                sampleScale += GRABPIXEL(0.15, -1.0);
                sampleScale += GRABPIXEL(0.18,  0.0);
                sampleScale += GRABPIXEL(0.15, +1.0);
                sampleScale += GRABPIXEL(0.12, +2.0);
                sampleScale += GRABPIXEL(0.09, +3.0);
                sampleScale += GRABPIXEL(0.05, +4.0);

                // Get the alpha to be drawn based on the dot of the normal and camera directions.
                half Rim = 1 - saturate(dot(normalize(i.viewDir), i.normal));
                half4 RimOut = _OcclusionColor * pow(Rim, _RimPower);

                finalColor = (sampleScale * RimOut) + ((1 - sampleScale) * finalColor);

                return half4 (finalColor);
            }
 
            ENDCG
        }
    }
}