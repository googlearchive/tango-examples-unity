/*
 * Copyright 2016 Google Inc. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
 
Shader "Custom/ZTestBlur"
{
    Properties
    {
        _CameraDepthTexture ("Camera Depth Texture", 2D) = "white" {}
        _MainTex ("Main Texture", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _GlossMap ("Gloss (R)", 2D) = "black" {}
        _GlossScale ("GlossScale", Range(0, 100)) = 0
        _Specular ("Specular", Range(0, 1)) = 0
        _BlurThresholdMax("Highlight Threshold Max", Range (0.001, 0.01)) = 0.01
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Power", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Geometry-10" "RenderType"="Opaque" }
        LOD 200
        
        // Non-convex meshes can have draw-order problems when dealing with transparency.
        // Perform a pass that forces an initial render to the depth buffer, and then offset
        // to deal with z-fighting when writing color.
        Pass
        {
            ZWrite On
            ColorMask 0
            Offset 1, 1
        }
        
        CGPROGRAM
        #pragma surface surf Lambert alpha:blend

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0
        
        sampler2D _CameraDepthTexture;
        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _GlossMap;
        float _GlossScale;
        float _Specular;
        float _BlurThresholdMax;
        float4 _RimColor;
        float _RimPower;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_GlossMap;
            float2 uv_BumpMap;
            float3 viewDir;
            float4 screenPos;
            float3 worldPos;
        };
        
        #include "TangoOcclusion.cginc"
        
        void surf(Input IN, inout SurfaceOutput o)
        {
            // Get the non-occluded color from the texture.
            float4 c = tex2D(_MainTex, IN.uv_MainTex);
            
            // Get the occluded color with by coloring the rim.
            o.Normal = UnpackNormal(tex2D (_BumpMap, IN.uv_BumpMap));
            float rim = 1 - saturate(dot(IN.viewDir, o.Normal));
            float4 rimOut = _RimColor * pow(rim, _RimPower);
            
            // Find the final color by blending the occluded and non-occluded colors.
            float occ = TangoOcclusionCertainty(_CameraDepthTexture, IN.screenPos, _BlurThresholdMax);
            c = (occ * rimOut) + ((1 - occ) * c);
            
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Specular = _Specular;
            o.Gloss = tex2D(_GlossMap, IN.uv_GlossMap).r * _GlossScale;
        }
        ENDCG
    } 
    FallBack "Diffuse"
}