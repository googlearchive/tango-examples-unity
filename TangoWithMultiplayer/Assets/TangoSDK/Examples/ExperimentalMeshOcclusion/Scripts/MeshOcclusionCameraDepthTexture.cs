//-----------------------------------------------------------------------
// <copyright file="MeshOcclusionCameraDepthTexture.cs" company="Google">
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
using UnityEngine;

/// <summary>
/// Attach and assign to the camera that will be generating the depth texture.
/// </summary>
[RequireComponent(typeof(Camera))]
public class MeshOcclusionCameraDepthTexture : MonoBehaviour
{
    /// <summary>
    /// The parent camera to match camera properties.
    /// </summary>
    public Camera m_parentCamera;

    /// <summary>
    /// The camera used to generate texture.
    /// </summary>
    private Camera m_camera;

    /// <summary>
    /// This function is called when the object becomes enabled and active.
    /// </summary>
    public void OnEnable()
    {
        // Setup attached camera properties.
        m_camera = GetComponent<Camera>();
        m_camera.depthTextureMode = DepthTextureMode.Depth;
        m_camera.targetTexture.width = Screen.width;
        m_camera.targetTexture.height = Screen.height;

        // Sync camera properties with parent.
        if (m_parentCamera != null)
        {
            m_camera.fieldOfView = m_parentCamera.fieldOfView;
            m_camera.nearClipPlane = m_parentCamera.nearClipPlane;
            m_camera.farClipPlane = m_parentCamera.farClipPlane;
        }
    }
}