//-----------------------------------------------------------------------
// <copyright file="EmulatedEnvironmentRenderHelper.cs" company="Google">
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
namespace Tango
{
#if UNITY_EDITOR
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Static class for helping with simulating output from Tango cameras (mainly Color/Depth) 
    /// in as unobtrusive a manner as possible (i.e., goes to lengths to avoid inserting things
    /// into the game scene that might reasonably affect it, such as meshes or physics objects).
    /// </summary>
    internal static class EmulatedEnvironmentRenderHelper
    {
        /// <summary>
        /// Runtime-instantiated camera used to render the simulated output.
        /// Will always be disabled, never used to render anything in the scene.
        /// </summary>
        public static Camera m_emulationCamera;

        /// <summary>
        /// Field of view of the emulated camera (both color and depth).
        /// </summary>
        private const float EMULATED_CAMERA_FOV = 37.8f;
        
        /// <summary>
        /// Name of shader resource used for depth emulation.
        /// </summary>
        private const string EMULATED_DEPTH_SHADERNAME = "Hidden/Tango/DepthEmulation";

        /// <summary>
        /// Name of shader resource used for color emulation.
        /// </summary>
        private const string EMULATED_COLORCAMERA_SHADERNAME = "Hidden/Tango/ColorCameraEmulation";
        
        /// <summary>
        /// Mesh to use as the emulated environment.
        /// </summary>
        private static Mesh m_renderObject;

        /// <summary>
        /// Material used to render environment as depth data for Tango emulation.
        /// </summary>
        private static Material m_emulatedDepthMaterial = null;

        /// <summary>
        /// Material used to render environment as color camera feed for Tango emulation.
        /// </summary>
        private static Material m_emulatedColorCameraMaterial = null;

        /// <summary>
        /// Selection of possible ways to render the emulated environment.
        /// </summary>
        public enum EmulatedDataType
        {
            DEPTH,
            COLOR_CAMERA
        }

        /// <summary>
        /// Set up the helper to render the given prefab as the emulated environment.
        /// </summary>
        /// <param name="environmentMesh">Mesh to render as environment.</param>
        /// <param name="environmentTexture">Texture to render environment mesh with.</param>
        /// <param name="cameraSimUsesLighting">Whether color camera emulation needs
        /// simulated lighting (e.g. an untextured mesh).</param>
        public static void InitForEnvironment(Mesh environmentMesh, Texture environmentTexture, bool cameraSimUsesLighting)
        {
            // Assign environment render data.
            m_renderObject = environmentMesh;

            // Create camera/depth emulation camera.
            if (m_emulationCamera == null)
            {
                m_emulationCamera = new GameObject().AddComponent<Camera>();
                m_emulationCamera.gameObject.name = "Tango Environment Emulation Camera";
                m_emulationCamera.fieldOfView = EMULATED_CAMERA_FOV;
                m_emulationCamera.enabled = false;
                GameObject.DontDestroyOnLoad(m_emulationCamera.gameObject);
            }

            // Create materials for rendering environment.
            if (m_emulatedColorCameraMaterial == null)
            {
                if (!CreateMaterialFromShaderName(EMULATED_COLORCAMERA_SHADERNAME,
                                                   out m_emulatedColorCameraMaterial))
                {
                    Debug.LogError("Could not find color camera emulation shader "
                                   + EMULATED_COLORCAMERA_SHADERNAME
                                   + ". Color camera emulation will not function as expected." 
                                   + " Video feed in editor will always be black.");
                }
            }
            
            if (m_emulatedDepthMaterial == null)
            {
                if (!CreateMaterialFromShaderName(EMULATED_DEPTH_SHADERNAME,
                                                   out m_emulatedDepthMaterial))
                {
                    Debug.LogError("Could not find depth emulation shader "
                                   + EMULATED_DEPTH_SHADERNAME
                                   + ". Depth emulation will produce only empty depth frames.");
                }
            }

            // Extra setup for color camera material:
            if (m_emulatedColorCameraMaterial != null)
            {
                // Assign texture.
                m_emulatedColorCameraMaterial.mainTexture = environmentTexture;
            
                // Set lighting feature for color camera emulation, on or off.
                if (cameraSimUsesLighting)
                {
                    m_emulatedColorCameraMaterial.EnableKeyword("LIT_TANGO_AR_EMULATION");
                }
                else
                {
                    m_emulatedColorCameraMaterial.DisableKeyword("LIT_TANGO_AR_EMULATION");
                }
            }
        }

        /// <summary>
        /// Reset any publicly visible elements of EmulatedEnvironmentRenderHelper
        /// to an equivalent-to-unintialized state.
        /// </summary>
        public static void Clear()
        {
            if (m_emulationCamera != null)
            {
                GameObject.DestroyImmediate(m_emulationCamera);
                m_emulationCamera = null;
            }
        }

        /// <summary>
        /// Render the emulated environment in the specified manner.
        /// </summary>
        /// <param name="renderTarget">Render target to render into.</param>
        /// <param name="dataTypeToRender">Type of data to render (e.g. color camera, depth data).</param>
        /// <param name="renderCameraPosition">Position (relative to emulated environment) 
        /// for the simulation camera to render from.</param>
        /// <param name="renderCameraRotation">Rotation (relative to emulated environment) 
        /// for the simulation camera to render at.</param>
        public static void RenderEmulatedEnvironment(RenderTexture renderTarget, EmulatedDataType dataTypeToRender,
                                                     Vector3 renderCameraPosition, Quaternion renderCameraRotation)
        {
            Material renderMaterial = null;
            switch (dataTypeToRender)
            {
            case EmulatedDataType.DEPTH:
                renderMaterial = m_emulatedDepthMaterial;
                break;
            case EmulatedDataType.COLOR_CAMERA:
                renderMaterial = m_emulatedColorCameraMaterial;
                break;
            }

            if (renderMaterial == null)
            {
                return;
            }

            // Set up camera
            m_emulationCamera.transform.position = renderCameraPosition;
            m_emulationCamera.transform.rotation = renderCameraRotation;
            
            // Stash current render state
            Camera previousCamera = Camera.current;
            RenderTexture previousRenderTexture = RenderTexture.active;
            
            // Set up new render state
            Camera.SetupCurrent(m_emulationCamera);

            Graphics.SetRenderTarget(renderTarget);
            GL.Clear(true, true, Color.black);

            // Render
            if(m_renderObject != null)
            {
                for (int i = 0; i < renderMaterial.passCount; i++)
                {
                    if (renderMaterial.SetPass(i))
                    {
                        Graphics.DrawMeshNow(m_renderObject, Matrix4x4.identity);
                    }
                }
            }
            
            // Restore previous render state
            Camera.SetupCurrent(previousCamera);
            RenderTexture.active = previousRenderTexture;
        }

        /// <summary>
        /// Find a shader needed for emulated environment rendering
        /// and create a material based off of it.
        /// </summary>
        /// <returns><c>true</c>, if the specified shader could be 
        /// found, <c>false</c> otherwise.</returns>
        /// <param name="shaderName">Shader name.</param>
        /// <param name="material">Material made from the shader (or, if the shader 
        /// could not be found, a material that outputs a constant (0,0,0,0)).</param>
        public static bool CreateMaterialFromShaderName(string shaderName,
                                                        out Material material)
        {
            Shader shader = Shader.Find(shaderName);

            if (shader == null)
            {
                // If can't find shader, use a material that will output (0, 0, 0, 0)
                material = new Material(Shader.Find("Hidden/Internal-Colored"));
                material.color = Color.clear;
                return false;
            }
            else
            {
                material = new Material(shader);
                return true;
            }
        }
    }
#endif
}