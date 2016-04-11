//-----------------------------------------------------------------------
// <copyright file="DepthProvider.cs" company="Google">
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
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using UnityEngine;

    /// <summary>
    /// C API wrapper for the Tango depth interface.
    /// </summary>
    internal class DepthProvider
    {
        private const float MIN_POINT_DISTANCE = 0.5f;
        private const float MAX_POINT_DISTANCE = 5f;
        private const int NUM_X_DEPTH_SAMPLES = 120;
        private const int NUM_Y_DEPTH_SAMPLES = 80;
        
        #if UNITY_EDITOR
        /// <summary>
        /// The emulated point cloud.  Used for Tango emulation on PC.
        /// </summary>
        private static List<Vector3> m_emulatedPointCloud = new List<Vector3>();
        
        /// <summary>
        /// Render target used to render environment for Tango emulation on PC.
        /// </summary>
        private static RenderTexture m_emulatedDepthTexture = null;
        
        /// <summary>
        /// Texture used to capture emulated depth info from render target.
        /// </summary>
        private static Texture2D m_emulationCaptureTexture = null;
        
        /// <summary>
        /// Shader used to render environment for Tango emulation on PC.
        /// </summary>
        private static Shader m_emulatedDepthShader = null;

        /// <summary>
        /// The theoretical capture time of most recent emulated depth frame. 
        /// </summary>
        private static float m_lastDepthEmulationTime;
        #endif

        /// <summary>
        /// Tango depth C callback function signature.
        /// </summary>
        /// <param name="callbackContext">Callback context.</param>
        /// <param name="xyzij">Depth information.</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void TangoService_onDepthAvailable(IntPtr callbackContext, [In, Out] TangoXYZij xyzij);

        /// <summary>
        /// Set the C callback for the Tango depth interface.
        /// </summary>
        /// <param name="callback">Callback method.</param>
        internal static void SetCallback(TangoService_onDepthAvailable callback)
        {
            int returnValue = DepthAPI.TangoService_connectOnXYZijAvailable(callback);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log("DepthProvider.SetCallback() Callback was not set!");
            }
            else
            {
                Debug.Log("DepthProvider.SetCallback() OnDepth callback was set!");
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// INTERNAL USE: Update the Tango emulation state for depth data.
        /// 
        /// Make this this is only called once per frame.
        /// </summary>
        internal static void UpdateTangoEmulation()
        {
            m_emulatedPointCloud.Clear();

            // Timestamp shall be something in the past, and we'll emulate the depth cloud based on it.
            m_lastDepthEmulationTime = PoseProvider.GetTimestampForDepthEmulation();

            // Get emulated position and rotation in Unity space.
            TangoPoseData poseData = new TangoPoseData();
            TangoCoordinateFramePair pair;
            pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
            pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE; 

            PoseProvider.GetPoseAtTime(poseData, m_lastDepthEmulationTime, pair);
            if (poseData.status_code != TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID)
            {
                return;
            }

            Vector3 position;
            Quaternion rotation;
            TangoSupport.TangoPoseToWorldTransform(poseData, out position, out rotation);

            // Instantiate any resources that we haven't yet.
            if (m_emulatedDepthTexture == null)
            {
                m_emulatedDepthTexture = new RenderTexture(NUM_X_DEPTH_SAMPLES, NUM_Y_DEPTH_SAMPLES, 24, RenderTextureFormat.ARGB32);
            }

            if (m_emulationCaptureTexture == null)
            {
                m_emulationCaptureTexture = new Texture2D(NUM_X_DEPTH_SAMPLES, NUM_Y_DEPTH_SAMPLES, TextureFormat.ARGB32, false);
            }

            if (m_emulatedDepthShader == null)
            {
                // Find depth shader by searching for it in project.
                string[] foundAssetGuids = UnityEditor.AssetDatabase.FindAssets("DepthEmulation t:Shader");
                if (foundAssetGuids.Length > 0)
                {
                    string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(foundAssetGuids[0]);
                    m_emulatedDepthShader = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, typeof(Shader)) as Shader;
                }
            }

            // Render emulated depth camera data.
            EmulatedEnvironmentRenderHelper.RenderEmulatedEnvironment(m_emulatedDepthTexture, m_emulatedDepthShader,
                                                                      position, rotation);
            
            // Capture rendered depth points from texture.
            RenderTexture.active = m_emulatedDepthTexture;
            m_emulationCaptureTexture.ReadPixels(new Rect(0, 0, m_emulatedDepthTexture.width, m_emulatedDepthTexture.height), 0, 0);
            m_emulationCaptureTexture.Apply();
            
            // Exctract captured data.
            Color32[] depthDataAsColors = m_emulationCaptureTexture.GetPixels32();

            // Convert depth texture to positions in camera space.
            Matrix4x4 projectionMatrix = GL.GetGPUProjectionMatrix(EmulatedEnvironmentRenderHelper.m_emulationCamera.projectionMatrix, false);
            Matrix4x4 reverseProjectionMatrix = projectionMatrix.inverse;

            float width = m_emulationCaptureTexture.width;
            float height = m_emulationCaptureTexture.height;
            for (int yTexel = 0; yTexel < height; yTexel++)
            {
                for (int xTexel = 0; xTexel < width; xTexel++)
                {
                    Color32 depthAsColor = depthDataAsColors[xTexel + (yTexel * m_emulationCaptureTexture.width)];
                    float clipSpaceZ = (depthAsColor.r - 128f) + (depthAsColor.g / 255f);

                    float ndcSpaceZ = (clipSpaceZ - projectionMatrix.m23) / projectionMatrix.m22;
                    float perspectiveDivisor = ndcSpaceZ * projectionMatrix.m32;

                    float ndcSpaceX = (((xTexel + 0.5f) / width) * 2f) - 1;
                    float ndcSpaceY = (((yTexel + 0.5f) / height) * 2f) - 1;

                    Vector4 clipSpacePos = new Vector4(ndcSpaceX * perspectiveDivisor, ndcSpaceY * perspectiveDivisor, clipSpaceZ, perspectiveDivisor);
                    Vector4 viewSpacePos = reverseProjectionMatrix * clipSpacePos;

                    Vector3 emulatedDepthPos = new Vector3(viewSpacePos.x, -viewSpacePos.y, -viewSpacePos.z);

                    if (emulatedDepthPos.z > MIN_POINT_DISTANCE && emulatedDepthPos.z < MAX_POINT_DISTANCE)
                    {
                        m_emulatedPointCloud.Add(emulatedDepthPos);
                    }
                }
            }
        }

        /// <summary>
        /// INTERNAL USE: Get the most recent values for Tango emulation.
        /// </summary>
        /// <returns>List of point cloud points.</returns>
        /// <param name="timestamp">Timestamp at which point cloud was captured.</param>
        internal static List<Vector3> GetTangoEmulation(out double timestamp)
        {
            timestamp = m_lastDepthEmulationTime;
            return m_emulatedPointCloud;
        }
#endif

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
                                                         "SA1600:ElementsMustBeDocumented",
                                                         Justification = "C API Wrapper.")]
        private struct DepthAPI
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            [DllImport(Common.TANGO_UNITY_DLL)]
            public static extern int TangoService_connectOnXYZijAvailable(TangoService_onDepthAvailable onDepthAvailalble);

 #else
            public static int TangoService_connectOnXYZijAvailable(TangoService_onDepthAvailable onDepthAvailalble)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
#endif
        }
    }
}
