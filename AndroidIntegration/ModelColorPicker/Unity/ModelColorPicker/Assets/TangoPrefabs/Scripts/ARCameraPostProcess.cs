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
/// Controls a shader which uses camera intrinsics to correct lens distortion.
/// 
/// Enabling this script will turn on lens distortion correction on the Tango
/// AR Camera prefab. Enabling the script will use more system resources, so
/// only enable it if your application requires it.
///
/// Part of the Tango AR Camera prefab.
/// </summary>
[RequireComponent(typeof(TangoARScreen))]
public class ARCameraPostProcess : MonoBehaviour
{
    /// <summary>
    /// Material of post-process shader that is to be run on the camera.
    /// </summary>
    public Material m_postProcessMaterial;

    /// <summary>
    /// Pass the camera intrinsics to both PostProcess and ARScreen shader.
    /// 
    /// The camera intrinsics are needed to distort and undo distort the camera image.
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
    }

    /// <summary>
    /// Unity OnEnable callback.
    /// </summary>
    private void OnEnable()
    {
        VideoOverlayProvider.SetARScreenDistortion(true);
    }

    /// <summary>
    /// Unity OnDisable callback.
    /// </summary>
    private void OnDisable()
    {
        VideoOverlayProvider.SetARScreenDistortion(false);
    }

    /// <summary>
    /// Unity OnRenderImage callback.
    /// </summary>
    /// <param name="src">The source image before processing.</param>
    /// <param name="dest">The destination image after processing.</param>
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(src, dest, m_postProcessMaterial);
    }
}
