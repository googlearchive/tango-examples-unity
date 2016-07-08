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
 
#ifndef TANGO_OCCLUSION_INCLUDED
#define TANGO_OCCLUSION_INCLUDED

// Return a weighted sample at the screen position by comparing the z depth in the depth texture and the camera distance.
// If the sampled depth is behind the camera, return the weight.
inline float TangoDepthSample(sampler2D cameraDepthTexture, float4 screenPos, float blurAmount, float cameraDistZ, float weight, float shiftX, float shiftY)
{
    float depthZ = LinearEyeDepth(tex2Dproj(cameraDepthTexture, UNITY_PROJ_COORD(screenPos + float4(shiftX * blurAmount, shiftY * blurAmount, 0, 0))).r);
    return (depthZ < cameraDistZ) * weight;
}

// Return the amount to occlude by performing a simple blur on the camera depth texture at the given screen position.
float TangoOcclusionCertainty(sampler2D cameraDepthTexture, float4 screenPos, float blurAmount)
{
    // Pre-calculate weights for each diagonal shift amount.
    float shift[5] = { 0.0, 1.0, 2.0, 3.0, 4.0 };
    float weight[5] = { 0.09, 0.075, 0.06, 0.045, 0.025 };
    float cameraDistZ = screenPos.z + (_ProjectionParams.y * 2);
    
    // Add up the weighted depth values by testing the z depth in diagonal directions to get a final value for the
    // amount that the screen position should be occluded.
    float certainty = 0;
    certainty += TangoDepthSample(cameraDepthTexture, screenPos, blurAmount, cameraDistZ, weight[0], shift[0], shift[0]);
    for (int i = 1; i < 5; i++){
        certainty += TangoDepthSample(cameraDepthTexture, screenPos, blurAmount, cameraDistZ, weight[i], shift[i], shift[i]);
        certainty += TangoDepthSample(cameraDepthTexture, screenPos, blurAmount, cameraDistZ, weight[i], -shift[i], shift[i]);
        certainty += TangoDepthSample(cameraDepthTexture, screenPos, blurAmount, cameraDistZ, weight[i], shift[i], -shift[i]);
        certainty += TangoDepthSample(cameraDepthTexture, screenPos, blurAmount, cameraDistZ, weight[i], -shift[i], -shift[i]);
    }
    return certainty;
}

#endif // TANGO_OCCLUSION_INCLUDED