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
#if UNITY_EDITOR
        /// <summary>
        /// INTERNAL USE: Flag set to true whenever emulated values have been updated.
        /// </summary>
        internal static bool m_emulationIsDirty;
#endif

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
        /// The theoretical capture time of most recent emulated depth frame.
        /// </summary>
        private static float m_lastDepthEmulationTime;

        /// <summary>
        /// Whether resources needed for emulation have been created.
        /// </summary>
        private static bool m_emulationIsInitialized = false;
#endif
        
        /// <summary>
        /// Tango point cloud C callback function signature.
        /// </summary>
        /// <param name="context">Callback context.</param>
        /// <param name="pointCloud">Point cloud data.</param> 
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void APIOnPointCloudAvailable(IntPtr context, ref TangoPointCloudIntPtr pointCloud);

        /// <summary>
        /// Set the C callback for the Tango depth interface.
        /// </summary>
        /// <param name="callback">Callback method.</param>
        internal static void SetCallback(APIOnPointCloudAvailable callback)
        {
            // The TangoCore in release-rana garbage initializes the PointCloudParcel
            // callback, causing crashes.  This fixes that issue.
            API.TangoServiceHidden_connectOnPointCloudParcelAvailable(IntPtr.Zero);

            int returnValue = API.TangoService_connectOnPointCloudAvailable(callback);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log("DepthProvider.SetCallback() Callback was not set!");
            }
            else
            {
                Debug.Log("DepthProvider.SetCallback() Callback was set!");
            }
        }

        /// <summary>
        /// Clear the C callback for the Tango depth interface.
        /// </summary>
        internal static void ClearCallback()
        {
            API.TangoServiceHidden_connectOnPointCloudParcelAvailable(IntPtr.Zero);
            int returnValue = API.TangoService_connectOnPointCloudAvailable(null);
            if (returnValue != Common.ErrorType.TANGO_SUCCESS)
            {
                Debug.Log("DepthProvider.ClearCallback() Callback was not cleared!");
            }
            else
            {
                Debug.Log("DepthProvider.ClearCallback() Callback was cleared!");
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// INTERNAL USE: Update the Tango emulation state for depth data.
        /// </summary>
        internal static void UpdateTangoEmulation()
        {
            m_emulatedPointCloud.Clear();

            // Timestamp shall be something in the past, and we'll emulate the depth cloud based on it.
            if (!PoseProvider.GetTimestampForDepthEmulation(out m_lastDepthEmulationTime))
            {
                Debug.LogError("Couldn't get a valid timestamp with which to emulate depth data. "
                               + "Depth emulation will be skipped this frame.");
                return;
            }

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
            _InternResourcesForEmulation();

            // Render emulated depth camera data.
            EmulatedEnvironmentRenderHelper.RenderEmulatedEnvironment(m_emulatedDepthTexture,
                                                                      EmulatedEnvironmentRenderHelper.EmulatedDataType.DEPTH,
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

            m_emulationIsDirty = true;
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

        /// <summary>
        /// Create any resources needed for emulation.
        /// </summary>
        private static void _InternResourcesForEmulation()
        {
            if(m_emulationIsInitialized)
            {
                return;
            }

            m_emulatedDepthTexture = new RenderTexture(NUM_X_DEPTH_SAMPLES, NUM_Y_DEPTH_SAMPLES, 24, 
                                                       RenderTextureFormat.ARGB32);
            m_emulationCaptureTexture = new Texture2D(NUM_X_DEPTH_SAMPLES, NUM_Y_DEPTH_SAMPLES, TextureFormat.ARGB32, 
                                                      false);

            m_emulationIsInitialized = true;
        }
#endif

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules",
                                                         "SA1600:ElementsMustBeDocumented",
                                                         Justification = "C API Wrapper.")]
        private struct API
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoService_connectOnPointCloudAvailable(APIOnPointCloudAvailable callback);

            [DllImport(Common.TANGO_CLIENT_API_DLL)]
            public static extern int TangoServiceHidden_connectOnPointCloudParcelAvailable(IntPtr callback);
 #else
            public static int TangoService_connectOnPointCloudAvailable(APIOnPointCloudAvailable callback)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }

            public static int TangoServiceHidden_connectOnPointCloudParcelAvailable(IntPtr callback)
            {
                return Common.ErrorType.TANGO_SUCCESS;
            }
#endif
        }
    }
}
