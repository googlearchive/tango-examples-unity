//-----------------------------------------------------------------------
// <copyright file="ARCameraPostProcess.cs" company="Google">
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
using System.Collections;
using Tango;
using UnityEngine;

/// <summary>
/// The post process distortion effect added on the virtual object.
/// 
/// Enable/disable this script will turn on/off the distortion effect, leave it to disabled it's distortion is not
/// crucial to your application.
/// </summary>
public class ARCameraPostProcess : MonoBehaviour
{
    /// <summary>
    /// The post process shader that is running on the camera.
    /// </summary>
    public Material m_postProcessMaterial;

    /// <summary>
    /// The AR screen material.
    /// 
    /// Needed to dynamically turn on / off the undistortion effect on the AR image.
    /// </summary>
    public Material m_arScreenMaterial;

    /// <summary>
    /// Pass the camera intrinsics to both PostProcess and ARScreen shader.
    /// 
    /// The camera intrinsics are needed for undistortion or distortion.
    /// </summary>
    /// <param name="intrinsics">Color camera intrinsics.</param>
    internal void SetupIntrinsic(TangoCameraIntrinsics intrinsics)
    {
        m_postProcessMaterial.SetFloat("_Width", (float)intrinsics.width);
        m_postProcessMaterial.SetFloat("_Height", (float)intrinsics.height);
        m_postProcessMaterial.SetFloat("_Fx", (float)intrinsics.fx);
        m_postProcessMaterial.SetFloat("_Fy", (float)intrinsics.fy);
        m_postProcessMaterial.SetFloat("_Cx", (float)intrinsics.cx);
        m_postProcessMaterial.SetFloat("_Cy", (float)intrinsics.cy);
        m_postProcessMaterial.SetFloat("_K0", (float)intrinsics.distortion0);
        m_postProcessMaterial.SetFloat("_K1", (float)intrinsics.distortion1);
        m_postProcessMaterial.SetFloat("_K2", (float)intrinsics.distortion2);
        
        m_arScreenMaterial.SetFloat("_TexWidth", (float)intrinsics.width);
        m_arScreenMaterial.SetFloat("_Height", (float)intrinsics.height);
        m_arScreenMaterial.SetFloat("_Fx", (float)intrinsics.fx);
        m_arScreenMaterial.SetFloat("_Fy", (float)intrinsics.fy);
        m_arScreenMaterial.SetFloat("_Cx", (float)intrinsics.cx);
        m_arScreenMaterial.SetFloat("_Cy", (float)intrinsics.cy);
        m_arScreenMaterial.SetFloat("_K0", (float)intrinsics.distortion0);
        m_arScreenMaterial.SetFloat("_K1", (float)intrinsics.distortion1);
        m_arScreenMaterial.SetFloat("_K2", (float)intrinsics.distortion2);
    }

    /// <summary>
    /// Unity OnEnable callback.
    /// 
    /// The distortion post process will be turned on after this script is enabled, at the same time, we need to
    /// change the distortion flag in AR Screen shader to true as well.
    /// </summary>
    private void OnEnable()
    {
        m_arScreenMaterial.EnableKeyword("DISTORTION_ON");
    }

    /// <summary>
    /// Unity OnEnable callback.
    /// 
    /// The reversed operation from the OnEnable() call.
    /// </summary>
    private void OnDisable()
    {
        m_arScreenMaterial.DisableKeyword("DISTORTION_ON");
    }

    /// <summary>
    /// Unity OnRenderImage callback.
    /// 
    /// Our customized post process shader will be excuted.
    /// </summary>
    /// <param name="src">The source image before processing.</param>
    /// <param name="dest">The destination image after processing.</param>
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(src, dest, m_postProcessMaterial);
    }
}