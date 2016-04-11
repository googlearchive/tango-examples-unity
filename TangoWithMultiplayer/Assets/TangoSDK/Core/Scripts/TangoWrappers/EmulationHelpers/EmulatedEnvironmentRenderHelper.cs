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
        /// Set of data, extracted from renderers and mesh filters,
        /// needed to render the emulated environment.
        /// </summary>
        private static List<EmulatedMeshRenderData> m_renderObjects;
        
        /// <summary>
        /// Callback to allow a class requesting a render to manipulate the material
        /// just prior to rendering a particular emulated environment object.
        /// </summary>
        /// <param name="material">Material the next environment object will be rendered with.</param>
        /// <param name="trsMatrix">Transform next environment object will be rendered at
        /// (in case that information is needed to set material matrix parameters).</param>
        public delegate void OnEmulatedMeshPreRender(Material material, Matrix4x4 trsMatrix);

        /// <summary>
        /// Set up the helper to render the given prefab as the emulated environment.
        /// </summary>
        /// <param name="environmentPrefab">Environment prefab.</param>
        public static void InitForEnvironment(GameObject environmentPrefab)
        {
            if (environmentPrefab == null)
            {
                return;
            }

            // Briefly instantiate environment to extract meshes, materials, transforms needed for rendering,
            // because GetComponent() cannot be called directly on a prefab.
            m_renderObjects = new List<EmulatedMeshRenderData>();
            GameObject environment = GameObject.Instantiate(environmentPrefab) as GameObject;
            MeshRenderer[] meshRenderers = environment.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                Mesh mesh = meshFilter ? meshFilter.mesh : null;
                Material mat = meshRenderer.material;
                
                if (mesh != null && mat != null)
                {
                    m_renderObjects.Add(new EmulatedMeshRenderData(mesh, mat, meshFilter.transform.localToWorldMatrix));
                }
            }

            GameObject.DestroyImmediate(environment);

            // Create camera/depth emulation camera.
            if (m_emulationCamera == null)
            {
                m_emulationCamera = new GameObject().AddComponent<Camera>();
                m_emulationCamera.gameObject.name = "Tango Environment Emulation Camera";
                m_emulationCamera.enabled = false;
            }
        }

        /// <summary>
        /// Render the emulated environment in the specified manner.
        /// </summary>
        /// <param name="renderTarget">Render target to render into. 
        /// When using more than one, the depth buffer of the first will be used.</param>
        /// <param name="shaderToRenderWith">Shader to render with. Note that Unity's lighting pipeline
        /// is bypassed here, so shaders utilizing Unity's lighting system will not render correctly.</param>
        /// <param name="renderPosition">Position (relative to emulated environment) 
        /// for the simulation camera to render from.</param>
        /// <param name="renderRotation">Rotation (relative to emulated environment) 
        /// for the simulation camera to render at.</param>
        /// <param name="perObjectRenderSetup">Optional callback to allow for changing state per-object
        /// (for example material properties) right before rendering.</param>
        public static void RenderEmulatedEnvironment(RenderTexture renderTarget, Shader shaderToRenderWith,
                                                     Vector3 renderPosition, Quaternion renderRotation,
                                                     OnEmulatedMeshPreRender perObjectRenderSetup = null)
        {
            // Set up camera
            m_emulationCamera.transform.position = renderPosition;
            m_emulationCamera.transform.rotation = renderRotation;
            
            // Stash current render state
            Camera previousCamera = Camera.current;
            RenderTexture previousRenderTexture = RenderTexture.active;
            
            // Set up new render state
            Camera.SetupCurrent(m_emulationCamera);

            Graphics.SetRenderTarget(renderTarget);
            GL.Clear(true, true, Color.black);
            
            // Render
            foreach (EmulatedMeshRenderData renderObject in m_renderObjects)
            {
                renderObject.m_material.shader = shaderToRenderWith;
                if (perObjectRenderSetup != null)
                {
                    perObjectRenderSetup(renderObject.m_material, renderObject.m_trsMatrix);
                }

                for (int i = 0; i < renderObject.m_material.passCount; i++)
                {
                    if (renderObject.m_material.SetPass(i))
                    {
                        Graphics.DrawMeshNow(renderObject.m_mesh, renderObject.m_trsMatrix);
                    }
                }
            }
            
            // Restore previous render state
            Camera.SetupCurrent(previousCamera);
            RenderTexture.active = previousRenderTexture;
        }

        /// <summary>
        /// Everything we need to know to render an object ourselves: a Mesh, a Material,
        /// and a world transform represented as a TRS matrix.
        /// </summary>
        private class EmulatedMeshRenderData
        {
            /// <summary>
            /// The mesh to render.
            /// </summary>
            public Mesh m_mesh;

            /// <summary>
            /// The material to render the mesh with.
            /// </summary>
            public Material m_material;

            /// <summary>
            /// A matrix representing the translation, rotation, and scale
            /// to render the object at.
            /// </summary>
            public Matrix4x4 m_trsMatrix;

            /// <summary>
            /// Initializes a new instance of the
            /// <see cref="Tango.EmulatedEnvironmentRenderHelper+EmulatedMeshRenderData"/> class.
            /// </summary>
            /// <param name="mesh">Mesh to render.</param>
            /// <param name="material">Material to render with.</param>
            /// <param name="trsMatrix">Translation-Rotation-Scale matrix (as in Unity's
            /// Transform.localToWorldMatrix).</param>
            public EmulatedMeshRenderData(Mesh mesh, Material material, Matrix4x4 trsMatrix)
            {
                m_mesh = mesh;
                m_material = material;
                m_trsMatrix = trsMatrix;
            }
        }
    }
#endif
}